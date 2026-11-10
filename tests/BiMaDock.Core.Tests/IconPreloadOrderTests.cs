using BiMaDock.Core.Icons;
using BiMaDock.Core.Model;

namespace BiMaDock.Core.Tests;

public sealed class IconPreloadOrderTests
{
    [Fact]
    public void Sort_MainFirst_ThenCategoryContents_ThenOrphans_NoCategoriesOrSeparators()
    {
        var cat = new DockEntry { Name = "Kat", Kind = DockEntryKind.Category };
        var layout = new DockLayout();
        var inCat = new DockEntry { Name = "inKat", Target = "c", CategoryId = cat.Id };
        var orphan = new DockEntry { Name = "Waise", Target = "o", CategoryId = Guid.NewGuid() };
        var main = new DockEntry { Name = "Haupt", Target = "m" };
        layout.Entries.AddRange([inCat, orphan, cat, new DockEntry { Name = "-", Kind = DockEntryKind.Separator }, main]);

        var order = IconPreloadOrder.Sort(layout).Select(e => e.Name).ToList();

        Assert.Equal(["Haupt", "inKat", "Waise"], order);
    }
}
