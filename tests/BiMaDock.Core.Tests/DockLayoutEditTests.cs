using BiMaDock.Core.Model;

namespace BiMaDock.Core.Tests;

public class DockLayoutEditTests
{
    private static DockLayout Sample(out DockEntry category)
    {
        category = new DockEntry { Name = "Kat", Kind = DockEntryKind.Category };
        var layout = new DockLayout
        {
            Entries =
            [
                new DockEntry { Name = "A" },
                category,
                new DockEntry { Name = "B" },
                new DockEntry { Name = "X", CategoryId = category.Id },
                new DockEntry { Name = "C" },
                new DockEntry { Name = "Y", CategoryId = category.Id },
            ],
        };
        return layout;
    }

    private static string[] Names(IEnumerable<DockEntry> entries) => entries.Select(e => e.Name).ToArray();

    private static Guid IdOf(DockLayout layout, string name) => layout.Entries.Single(e => e.Name == name).Id;

    [Theory]
    [InlineData(0, new[] { "N", "A", "Kat", "B", "C" })]
    [InlineData(2, new[] { "A", "Kat", "N", "B", "C" })]
    [InlineData(4, new[] { "A", "Kat", "B", "C", "N" })]
    [InlineData(99, new[] { "A", "Kat", "B", "C", "N" })]
    public void Insert_IntoMainDock_UsesPositionAmongMainEntries(int index, string[] expected)
    {
        var layout = Sample(out _);

        layout.Insert(new DockEntry { Name = "N" }, null, index);

        Assert.Equal(expected, Names(layout.MainEntries));
    }

    [Fact]
    public void Insert_IntoCategory_KeepsCategoryOrder()
    {
        var layout = Sample(out var category);

        layout.Insert(new DockEntry { Name = "N" }, category.Id, 1);

        Assert.Equal(["X", "N", "Y"], Names(layout.EntriesOf(category.Id)));
    }

    [Fact]
    public void Insert_CategoryIntoCategory_LandsInMainDock()
    {
        var layout = Sample(out var category);

        var inserted = layout.Insert(new DockEntry { Name = "K2", Kind = DockEntryKind.Category }, category.Id, 0);

        Assert.Null(inserted.CategoryId);
        Assert.Equal("K2", layout.MainEntries.First().Name);
    }

    [Fact]
    public void Move_WithinMainDock_IndexCountsWithoutMovedEntry()
    {
        var layout = Sample(out _);

        Assert.True(layout.Move(IdOf(layout, "A"), null, 2));

        Assert.Equal(["Kat", "B", "A", "C"], Names(layout.MainEntries));
    }

    [Fact]
    public void Move_FromMainIntoCategoryAndBack()
    {
        var layout = Sample(out var category);

        layout.Move(IdOf(layout, "B"), category.Id, int.MaxValue);
        Assert.Equal(["X", "Y", "B"], Names(layout.EntriesOf(category.Id)));
        Assert.Equal(["A", "Kat", "C"], Names(layout.MainEntries));

        layout.Move(IdOf(layout, "X"), null, 0);
        Assert.Equal(["X", "A", "Kat", "C"], Names(layout.MainEntries));
        Assert.Equal(["Y", "B"], Names(layout.EntriesOf(category.Id)));
    }

    [Fact]
    public void Move_CategoryIntoItself_IsRejected()
    {
        var layout = Sample(out var category);

        Assert.False(layout.Move(category.Id, category.Id, 0));
        Assert.False(layout.Move(Guid.NewGuid(), null, 0));
        Assert.Equal(6, layout.Entries.Count);
    }

    [Theory]
    [InlineData("https://www.gitea.io/de/docs", "gitea.io")]
    [InlineData("http://192.168.32.130:3000", "192.168.32.130")]
    [InlineData(@"C:\Tools\Modbus Scanner.exe", "Modbus Scanner")]
    [InlineData(@"C:\Users\Marco\Desktop\Excel.lnk", "Excel")]
    [InlineData(@"D:\", "D:")]
    public void Factory_DerivesNameAndTarget(string value, string expectedName)
    {
        var entry = DockEntryFactory.FromDropValue(value);

        Assert.NotNull(entry);
        Assert.Equal(expectedName, entry.Name);
        Assert.Equal(DockEntryKind.Launch, entry.Kind);
    }

    [Fact]
    public void Factory_UsesFolderNameForDirectories()
    {
        var dir = Directory.CreateTempSubdirectory("Projekt.v2-");
        try
        {
            Assert.Equal(dir.Name, DockEntryFactory.FromDropValue(dir.FullName)!.Name);
        }
        finally
        {
            dir.Delete();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nur Text")]
    [InlineData("ftp://server/datei")]
    public void Factory_RejectsUnsupportedValues(string? value) => Assert.Null(DockEntryFactory.FromDropValue(value));
}
