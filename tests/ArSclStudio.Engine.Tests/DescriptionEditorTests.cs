using ArSclStudio.Desktop.ViewModels;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class DescriptionEditorTests
{
    [TestMethod]
    public async Task DesktopEditsThroughEngineAndRefreshesUndoChangesAndSelection()
    {
        await using var fixture = await EditingFixture.CreateAsync();
        await using var vm = new MainWindowViewModel();
        await vm.OpenFileAsync(fixture.Path);
        var ied = vm.EngineeringRows.First(row => row.Name == "Relay_A");
        vm.SelectedEngineeringRow = ied;
        Assert.IsTrue(vm.CanEdit);
        vm.BeginDescriptionEdit();
        vm.DescriptionDraft = "UI description";
        Assert.IsTrue(vm.HasUnsavedChanges);
        Assert.IsFalse(vm.CanSave);
        await vm.ApplyDescriptionAsync();
        Assert.IsTrue(vm.IsDirty);
        Assert.IsTrue(vm.CanUndo);
        Assert.AreEqual("UI description", vm.ChangeRows[0].After);
        Assert.AreEqual(ied.Handle, vm.SelectedEngineeringRow!.Handle);
        await vm.UndoAsync();
        Assert.IsFalse(vm.IsDirty);
        Assert.IsTrue(vm.CanRedo);
        await vm.RedoAsync();
        Assert.IsTrue(await vm.SaveAsync());
        Assert.IsFalse(vm.HasUnsavedChanges);
        await vm.OpenFileAsync(fixture.Path);
        Assert.IsFalse(vm.CanUndo);
        Assert.AreEqual(0, vm.ChangeRows.Count);
        Assert.AreEqual("SCL", vm.DetailTitle);
    }

    [TestMethod]
    public async Task DraftRemainsBoundToOriginalIedDuringNavigationAndCancelDoesNotMutate()
    {
        await using var fixture = await EditingFixture.CreateAsync();
        await using var vm = new MainWindowViewModel();
        await vm.OpenFileAsync(fixture.Path);
        vm.SelectedEngineeringRow = vm.EngineeringRows.First(row => row.Name == "Relay_A");
        vm.BeginDescriptionEdit();
        vm.DescriptionDraft = "A only";
        vm.SelectedEngineeringRow = vm.EngineeringRows.First(row => row.Name == "Relay_B");
        Assert.AreEqual("Relay_A", vm.EditingTargetName);
        await vm.ApplyDescriptionAsync();
        vm.BeginDescriptionEdit();
        Assert.AreEqual("", vm.DescriptionDraft);
        vm.DescriptionDraft = "discard me";
        vm.CancelDescriptionEdit();
        Assert.AreEqual(1, vm.ChangeRows.Count);
    }
}
