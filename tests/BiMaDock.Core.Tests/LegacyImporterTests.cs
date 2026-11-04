using BiMaDock.Core.Model;
using BiMaDock.Core.Storage;

namespace BiMaDock.Core.Tests;

public class LegacyImporterTests
{
    private const string CategoryId = "ef40c9ee-8a5b-434f-a54b-31bfc417ef97";

    private const string LegacyJson = $$"""
        [
          { "Id": "b19cc919-68a0-4a7b-b57a-fc3011b1f495", "DisplayName": "Scanner", "FilePath": "C:\\Tools\\scan.exe", "Category": "", "IsCategory": false, "Position": 2, "IconSource": "" },
          { "Id": "{{CategoryId}}", "DisplayName": "3D", "FilePath": "", "Category": "", "IsCategory": true, "Position": 0, "IconSource": "C:/Users/Marco/AppData/Local/BiMaDock/Icons/print.png" },
          { "Id": "c10d17d8-d727-46b7-be35-8a23d64f4384", "DisplayName": "Slicer", "FilePath": "C:\\Slicer\\slicer.exe", "Category": "{{CategoryId}}", "IsCategory": false, "Position": 0, "IconSource": "" },
          { "Id": "11111111-1111-1111-1111-111111111111", "DisplayName": "", "FilePath": "C:\\Data\\notes.txt", "Category": "deadbeef-0000-0000-0000-000000000000", "IsCategory": false, "Position": 1, "IconSource": "" }
        ]
        """;

    [Fact]
    public void Import_KeepsIdsAndCategoryMembership()
    {
        var layout = LegacyImporter.Import(LegacyJson);

        var category = Assert.Single(layout.Entries, e => e.IsCategory);
        Assert.Equal(Guid.Parse(CategoryId), category.Id);
        var child = Assert.Single(layout.EntriesOf(category.Id));
        Assert.Equal("Slicer", child.Name);
    }

    [Fact]
    public void Import_OrdersMainEntriesByPosition()
    {
        var layout = LegacyImporter.Import(LegacyJson);

        Assert.Equal(["3D", "notes", "Scanner"], layout.MainEntries.Select(e => e.Name));
    }

    [Fact]
    public void Import_MovesOrphansToMainDockAndDerivesMissingNames()
    {
        var layout = LegacyImporter.Import(LegacyJson);

        var orphan = layout.Find(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        Assert.NotNull(orphan);
        Assert.Null(orphan.CategoryId);
        Assert.Equal("notes", orphan.Name);
    }

    [Fact]
    public void Import_ConvertsForwardSlashIconPaths()
    {
        var layout = LegacyImporter.Import(LegacyJson);

        var category = layout.Entries.Single(e => e.IsCategory);
        Assert.Equal(@"C:\Users\Marco\AppData\Local\BiMaDock\Icons\print.png", category.IconPath);
        Assert.Null(layout.Entries.Single(e => e.Name == "Scanner").IconPath);
    }

    [Fact]
    public void Import_KeepsWebLinksAsTarget()
    {
        var layout = LegacyImporter.Import("""[{ "Id": "x", "DisplayName": "Gitea", "FilePath": "http://192.168.32.130:3000", "IsCategory": false, "Position": 0 }]""");

        var entry = Assert.Single(layout.Entries);
        Assert.Equal("http://192.168.32.130:3000", entry.Target);
        Assert.Equal(DockEntryKind.Launch, entry.Kind);
        Assert.NotEqual(Guid.Empty, entry.Id);
    }
}
