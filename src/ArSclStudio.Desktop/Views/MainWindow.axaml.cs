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

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel
                .DisposeAsync()
                .AsTask()
                .GetAwaiter()
                .GetResult();
        }

        base.OnClosed(e);
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

        await viewModel.OpenFileAsync(files[0].Path.LocalPath);
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
