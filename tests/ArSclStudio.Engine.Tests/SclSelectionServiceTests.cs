using ArSclStudio.Engine.Navigation;
using ArSclStudio.Scl.Identity;

namespace ArSclStudio.Engine.Tests;

[TestClass]
public sealed class SclSelectionServiceTests
{
    [TestMethod]
    public void SelectPublishesOnlyRealChangedHandles()
    {
        var service = new SclSelectionService();
        var notifications = 0;
        var selected = SclNodeHandle.None;

        service.SelectionChanged += (_, args) =>
        {
            notifications++;
            selected = args.SelectedNode;
        };

        Assert.IsFalse(service.Select(SclNodeHandle.None));
        Assert.IsTrue(service.Select(new SclNodeHandle(42)));
        Assert.IsFalse(service.Select(new SclNodeHandle(42)));

        Assert.AreEqual(1, notifications);
        Assert.AreEqual(new SclNodeHandle(42), selected);
    }
}
