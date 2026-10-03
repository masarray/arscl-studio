using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ArSclStudio.Desktop.ViewModels;

namespace ArSclStudio.Desktop.Views;

public sealed partial class MainWindow : Window
{
    private static readonly FilePickerFileType SclFileType = new("IEC 61850 SCL")
    {
        Patterns =
        [
            "*.icd",
            "*.iid",
            "*.cid",
            "*.scd",
            "*.ssd",
            "*.sed",
            "*.xml"
        ]
    };

    private bool _closeConfirmed;
    private bool _closeRequested;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (_closeConfirmed || DataContext is not MainWindowViewModel viewModel) { return; }
        e.Cancel = true;
        if (_closeRequested || viewModel.IsBusy) { return; }
        _closeRequested = true;
        try
        {
            if (!await ConfirmReplacementAsync(viewModel)) { return; }
            await viewModel.DisposeAsync();
            _closeConfirmed = true;
            Close();
        }
        catch (Exception exception)
        {
            viewModel.StatusText = $"Could not close workspace: {exception.Message}";
        }
        finally { _closeRequested = false; }
    }

    private async Task<bool> ConfirmReplacementAsync(MainWindowViewModel viewModel)
    {
        if (!viewModel.HasUnsavedChanges) { return true; }
        var dialog = new Window
        {
            Title = "Unsaved changes", Width = 430, Height = 170, CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var save = new Button { Content = "Save", IsEnabled = !viewModel.IsEditing };
        var discard = new Button { Content = "Discard" };
        var cancel = new Button { Content = "Cancel" };
        save.Click += (_, _) => dialog.Close("save");
        discard.Click += (_, _) => dialog.Close("discard");
        cancel.Click += (_, _) => dialog.Close("cancel");
        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(16), Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = viewModel.IsEditing
                        ? "Apply or cancel the description draft before saving."
                        : "Save the document changes before continuing?",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8,
                    Children = { save, discard, cancel }
                }
            }
        };
        var choice = await dialog.ShowDialog<string?>(this);
        return choice == "discard" || (choice == "save" && await viewModel.SaveAsync());
    }

    private void EditDescriptionClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm) { vm.BeginDescriptionEdit(); }
    }

    private void CancelDescriptionClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm) { vm.CancelDescriptionEdit(); }
    }

    private async void ApplyDescriptionClick(object? sender, RoutedEventArgs e) =>
        await RunOperationAsync(static vm => vm.ApplyDescriptionAsync());

    private async void UndoClick(object? sender, RoutedEventArgs e) =>
        await RunOperationAsync(static vm => vm.UndoAsync());

    private async void RedoClick(object? sender, RoutedEventArgs e) =>
        await RunOperationAsync(static vm => vm.RedoAsync());

    private async void SaveClick(object? sender, RoutedEventArgs e) =>
        await RunOperationAsync(async vm => { await vm.SaveAsync(); });

    private async void SaveAsClick(object? sender, RoutedEventArgs e)
    {
        await RunOperationAsync(async vm =>
        {
            if (!vm.CanSave) { return; }
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save SCL As", SuggestedFileName = vm.CurrentFileName,
                ShowOverwritePrompt = true, FileTypeChoices = [SclFileType]
            });
            if (file is not null) { await vm.SaveAsync(file.Path.LocalPath, overwrite: true); }
        });
    }

    private async Task RunOperationAsync(Func<MainWindowViewModel, Task> operation)
    {
        if (DataContext is not MainWindowViewModel vm) { return; }
        try { await operation(vm); }
        catch (ObjectDisposedException) { /* Window lifetime ended. */ }
        catch (Exception exception) { vm.StatusText = $"Operation failed: {exception.Message}"; }
    }

    private async void OpenFileClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel ||
            viewModel.IsBusy)
        {
            return;
        }

        await RunOperationAsync(async _ =>
        {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Open IEC 61850 SCL",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    SclFileType,
                    FilePickerFileTypes.All
                ]
            });

        if (files.Count != 1)
        {
            return;
        }

        if (await ConfirmReplacementAsync(viewModel))
        {
            await viewModel.OpenFileAsync(files[0].Path.LocalPath);
        }
        });
    }

    private async void SearchTextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox ||
            DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        try
        {
            await viewModel.SearchAsync(textBox.Text);
        }
        catch (ObjectDisposedException)
        {
            // Window/session is closing.
        }
    }

    private void EngineeringToggleClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ExplorerRow row } &&
            DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ToggleEngineering(row);
            e.Handled = true;
        }
    }

    private void XmlToggleClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ExplorerRow row } &&
            DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ToggleXml(row);
            e.Handled = true;
        }
    }

    private void ExitClick(
        object? sender,
        RoutedEventArgs e) =>
        Close();
}

