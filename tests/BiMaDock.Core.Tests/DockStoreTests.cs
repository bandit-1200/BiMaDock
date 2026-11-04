using BiMaDock.Core.Model;
using BiMaDock.Core.Storage;

namespace BiMaDock.Core.Tests;

public sealed class DockStoreTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("bimadock-tests-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string PathOf(string name) => Path.Combine(_dir, name);

    [Fact]
    public void SaveThenLoad_RoundTripsEntries()
    {
        var store = new DockStore(PathOf("dock.json"));
        var category = new DockEntry { Name = "Tools", Kind = DockEntryKind.Category, IconPath = @"C:\icon.png" };
        var layout = new DockLayout
        {
            Entries = [category, new DockEntry { Name = "Notepad", Target = @"C:\Windows\notepad.exe", CategoryId = category.Id }],
        };

        store.Save(layout);
        var loaded = store.Load();

        Assert.Equal(layout.Entries, loaded.Entries);
        Assert.False(File.Exists(PathOf("dock.json.tmp")));
    }

    [Fact]
    public void Load_WithoutAnyFile_ReturnsEmptyLayout()
    {
        var loaded = new DockStore(PathOf("dock.json"), PathOf("missing-legacy.json")).Load();

        Assert.Empty(loaded.Entries);
    }

    [Fact]
    public void Load_CorruptFile_IsBackedUpAndReturnsEmptyLayout()
    {
        File.WriteAllText(PathOf("dock.json"), "{ kaputt");

        var loaded = new DockStore(PathOf("dock.json")).Load();

        Assert.Empty(loaded.Entries);
        Assert.Single(Directory.GetFiles(_dir, "dock.json.corrupt-*"));
    }

    [Fact]
    public void Load_FirstStart_ImportsLegacyFileOnceAndLeavesItUntouched()
    {
        const string legacy = """[{ "Id": "a", "DisplayName": "Explorer", "FilePath": "C:\\Windows\\explorer.exe", "Position": 0 }]""";
        File.WriteAllText(PathOf("docksettings.json"), legacy);
        var store = new DockStore(PathOf("dock.json"), PathOf("docksettings.json"));

        var first = store.Load();

        Assert.Equal("Explorer", Assert.Single(first.Entries).Name);
        Assert.True(File.Exists(PathOf("dock.json")));
        Assert.Equal(legacy, File.ReadAllText(PathOf("docksettings.json")));

        // Weitere Starts lesen die neue Datei, nicht mehr die alte.
        File.WriteAllText(PathOf("docksettings.json"), "[]");
        Assert.Single(store.Load().Entries);
    }

    [Fact]
    public void Normalize_FixesNestedCategoriesAndDuplicateIds()
    {
        var id = Guid.NewGuid();
        var outer = new DockEntry { Name = "A", Kind = DockEntryKind.Category };
        var layout = new DockLayout
        {
            Entries =
            [
                outer,
                new DockEntry { Name = "B", Kind = DockEntryKind.Category, CategoryId = outer.Id },
                new DockEntry { Id = id, Name = "C" },
                new DockEntry { Id = id, Name = "D" },
            ],
        };

        Assert.Equal(2, layout.Normalize());
        Assert.All(layout.Entries.Where(e => e.IsCategory), e => Assert.Null(e.CategoryId));
        Assert.Equal(4, layout.Entries.Select(e => e.Id).Distinct().Count());
    }
}
