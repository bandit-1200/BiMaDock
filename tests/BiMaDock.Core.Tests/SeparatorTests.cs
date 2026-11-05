using BiMaDock.Core.Cleanup;
using BiMaDock.Core.Model;
using BiMaDock.Core.Storage;

namespace BiMaDock.Core.Tests;

public sealed class SeparatorTests
{
    [Fact]
    public void Cleanup_IgnoresSeparators_AndTreatsCategoryWithOnlySeparatorsAsEmpty()
    {
        var category = new DockEntry { Name = "Nur Trenner", Kind = DockEntryKind.Category };
        var layout = new DockLayout
        {
            Entries =
            [
                category,
                DockEntryFactory.NewSeparator(),
                DockEntryFactory.NewSeparator() with { CategoryId = category.Id },
            ],
        };

        var result = CleanupScanner.Scan(layout, _ => PathState.Missing);

        Assert.Equal(["Nur Trenner:EmptyCategory"], result.Select(c => $"{c.Entry.Name}:{c.Reason}"));
    }

    [Fact]
    public void Insert_KeepsSeparatorInCategory()
    {
        var category = DockEntryFactory.NewCategory("Kat");
        var layout = new DockLayout { Entries = [category] };

        var separator = layout.Insert(DockEntryFactory.NewSeparator(), category.Id, 0);

        Assert.Equal(category.Id, separator.CategoryId);
        Assert.True(separator.IsSeparator);
    }

    [Fact]
    public void Store_RoundTripsSeparator()
    {
        string dir = Directory.CreateTempSubdirectory("bimadock-sep-").FullName;
        try
        {
            var store = new DockStore(Path.Combine(dir, "dock.json"));
            store.Save(new DockLayout { Entries = [DockEntryFactory.NewSeparator()] });

            Assert.True(Assert.Single(store.Load().Entries).IsSeparator);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Cleanup_FindsDuplicatesInSameDockOnly_KeepsFirst()
    {
        var category = DockEntryFactory.NewCategory("Kat");
        var first = new DockEntry { Name = "cmd 1", Target = @"C:\Windows\cmd.exe" };
        var layout = new DockLayout
        {
            Entries =
            [
                category,
                first,
                new DockEntry { Name = "cmd 2", Target = @"c:\windows\CMD.EXE" },
                new DockEntry { Name = "cmd in Kat", Target = @"C:\Windows\cmd.exe", CategoryId = category.Id },
            ],
        };

        var result = CleanupScanner.Scan(layout, _ => PathState.Exists);

        Assert.Equal(["cmd 2:Duplicate:True"], result.Select(c => $"{c.Entry.Name}:{c.Reason}:{c.SelectedByDefault}"));
    }
}
