using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace BiMaDock.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DataDirectoryCollection
{
    public const string Name = "DataDir";
}

public sealed class DockCleanupTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"BiMaDock.Tests.{Guid.NewGuid():N}");

    public DockCleanupTests()
    {
        Directory.CreateDirectory(directory);
    }

    [Fact]
    public async Task FindCandidatesAsync_IgnoresExistingFileAndDirectory()
    {
        var file = Path.Combine(directory, "tool.exe");
        File.WriteAllText(file, string.Empty);
        var folder = Path.Combine(directory, "Ordner");
        Directory.CreateDirectory(folder);

        var candidates = await DockCleanup.FindCandidatesAsync(new[] { Item("Tool", file, 0), Item("Ordner", folder, 1) });

        Assert.Empty(candidates);
    }

    [Fact]
    public async Task FindCandidatesAsync_ReportsMissingFileAsPreselected()
    {
        var missing = Item("Fehlt", Path.Combine(directory, "fehlt.exe"), 0);

        var candidates = await DockCleanup.FindCandidatesAsync(new[] { missing });

        var candidate = Assert.Single(candidates);
        Assert.Same(missing, candidate.Item);
        Assert.Equal(CleanupReason.FileMissing, candidate.Reason);
        Assert.True(candidate.PreSelected);
        Assert.Equal("Hauptdock", candidate.CategoryName);
        Assert.Equal("Datei oder Ordner nicht gefunden", candidate.Description);
    }

    [Fact]
    public async Task FindCandidatesAsync_ReportsEmptyFilePathAsMissing()
    {
        var candidates = await DockCleanup.FindCandidatesAsync(new[] { Item("Leer", "  ", 0) });

        Assert.Equal(CleanupReason.FileMissing, Assert.Single(candidates).Reason);
    }

    [Theory]
    [InlineData("https://example.com/")]
    [InlineData("http://example.com/seite")]
    [InlineData("mailto:test@example.com")]
    public async Task FindCandidatesAsync_SkipsUrls(string url)
    {
        var candidates = await DockCleanup.FindCandidatesAsync(new[] { Item("Web", url, 0) });

        Assert.Empty(candidates);
    }

    [Fact]
    public async Task FindCandidatesAsync_ReportsCategoryWithoutChildren()
    {
        var category = Category("Leer", 0);

        var candidates = await DockCleanup.FindCandidatesAsync(new[] { category });

        var candidate = Assert.Single(candidates);
        Assert.Same(category, candidate.Item);
        Assert.Equal(CleanupReason.EmptyCategory, candidate.Reason);
        Assert.False(candidate.PreSelected);
        Assert.Equal("Hauptdock", candidate.CategoryName);
        Assert.Equal("Kategorie ist leer", candidate.Description);
    }

    [Fact]
    public async Task FindCandidatesAsync_ReportsCategoryWhoseChildrenAreAllMissing()
    {
        var category = Category("Werkzeuge", 0);
        var first = Item("Eins", Path.Combine(directory, "eins.exe"), 1, category.Id);
        var second = Item("Zwei", Path.Combine(directory, "zwei.exe"), 0, category.Id);

        var candidates = await DockCleanup.FindCandidatesAsync(new[] { category, first, second });

        Assert.Equal(3, candidates.Count);
        Assert.Same(second, candidates[0].Item);
        Assert.Same(first, candidates[1].Item);
        Assert.All(candidates.Take(2), candidate =>
        {
            Assert.Equal(CleanupReason.FileMissing, candidate.Reason);
            Assert.Equal("Werkzeuge", candidate.CategoryName);
        });
        Assert.Same(category, candidates[2].Item);
        Assert.Equal(CleanupReason.EmptyCategory, candidates[2].Reason);
        Assert.Equal("Kategorie wäre nach dem Aufräumen leer", candidates[2].Description);
        Assert.False(candidates[2].PreSelected);
    }

    [Fact]
    public async Task FindCandidatesAsync_KeepsCategoryWithExistingChild()
    {
        var file = Path.Combine(directory, "da.exe");
        File.WriteAllText(file, string.Empty);
        var category = Category("Werkzeuge", 0);
        var existing = Item("Da", file, 0, category.Id);
        var missing = Item("Fehlt", Path.Combine(directory, "fehlt.exe"), 1, category.Id);

        var candidates = await DockCleanup.FindCandidatesAsync(new[] { category, existing, missing });

        var candidate = Assert.Single(candidates);
        Assert.Same(missing, candidate.Item);
    }

    [Fact]
    public async Task FindCandidatesAsync_OrdersMainDockThenCategoryItemsThenCategories()
    {
        var category = Category("Kat", 0);
        var mainLater = Item("B", Path.Combine(directory, "b.exe"), 5);
        var mainFirst = Item("A", Path.Combine(directory, "a.exe"), 1);
        var child = Item("C", Path.Combine(directory, "c.exe"), 0, category.Id);

        var candidates = await DockCleanup.FindCandidatesAsync(new[] { category, child, mainLater, mainFirst });

        Assert.Equal(new[] { mainFirst, mainLater, child, category }, candidates.Select(candidate => candidate.Item));
    }

    [Fact]
    public async Task FindCandidatesAsync_ReportsShortcutWithMissingTarget()
    {
        var target = Path.Combine(directory, "ziel.exe");
        File.WriteAllText(target, string.Empty);
        var shortcut = Path.Combine(directory, "ziel.lnk");
        if (!TryCreateShortcut(shortcut, target))
        {
            return; // WScript.Shell nicht verfügbar
        }

        var item = Item("Verknüpfung", shortcut, 0);
        Assert.Empty(await DockCleanup.FindCandidatesAsync(new[] { item }));

        File.Delete(target);
        var candidates = await DockCleanup.FindCandidatesAsync(new[] { item });

        var candidate = Assert.Single(candidates);
        Assert.Equal(CleanupReason.ShortcutTargetMissing, candidate.Reason);
        Assert.True(candidate.PreSelected);
        Assert.StartsWith("Verknüpfungsziel nicht gefunden: ", candidate.Description);
        Assert.Contains("ziel.exe", candidate.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FindCandidatesAsync_ReportsMissingShortcutFileAsMissing()
    {
        var candidates = await DockCleanup.FindCandidatesAsync(new[] { Item("Weg", Path.Combine(directory, "weg.lnk"), 0) });

        Assert.Equal(CleanupReason.FileMissing, Assert.Single(candidates).Reason);
    }

    [Fact]
    public void RemoveItems_RemovesCategoryWithItsChildren()
    {
        var category = Category("Kat", 0);
        var child = Item("Kind", @"C:\kind.exe", 0, category.Id);
        var other = Item("Anders", @"C:\anders.exe", 1);

        var result = DockCleanup.RemoveItems(new[] { category, child, other }, new[] { category });

        Assert.Equal(new[] { other }, result);
    }

    [Fact]
    public void RemoveItems_MatchesById()
    {
        var item = Item("Eins", @"C:\eins.exe", 0);
        var keep = Item("Zwei", @"C:\zwei.exe", 1);
        var copy = new DockItem { Id = item.Id };

        var result = DockCleanup.RemoveItems(new[] { item, keep }, new[] { copy });

        Assert.Equal(new[] { keep }, result);
    }

    public void Dispose()
    {
        Directory.Delete(directory, recursive: true);
    }

    internal static DockItem Item(string name, string path, int position, string category = "") =>
        new() { DisplayName = name, FilePath = path, Position = position, Category = category };

    internal static DockItem Category(string name, int position) =>
        new() { DisplayName = name, IsCategory = true, Position = position };

    private static bool TryCreateShortcut(string shortcutPath, string targetPath)
    {
        Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null)
        {
            return false;
        }

        object? shell = null;
        object? shortcut = null;
        try
        {
            shell = Activator.CreateInstance(shellType);
            if (shell is null)
            {
                return false;
            }

            dynamic shellObject = shell;
            shortcut = shellObject.CreateShortcut(shortcutPath);
            dynamic shortcutObject = shortcut!;
            shortcutObject.TargetPath = targetPath;
            shortcutObject.Save();
            return File.Exists(shortcutPath);
        }
        catch (Exception exception) when (exception is COMException or InvalidCastException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
        {
            return false;
        }
        finally
        {
            if (shortcut is not null && Marshal.IsComObject(shortcut))
            {
                Marshal.FinalReleaseComObject(shortcut);
            }

            if (shell is not null && Marshal.IsComObject(shell))
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }
    }
}

[Collection(DataDirectoryCollection.Name)]
public sealed class DockCleanupBackupTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"BiMaDock.Tests.{Guid.NewGuid():N}");
    private readonly string? previousDataDirectory = Environment.GetEnvironmentVariable(AppPaths.DataDirectoryVariable);

    public DockCleanupBackupTests()
    {
        Directory.CreateDirectory(directory);
        Environment.SetEnvironmentVariable(AppPaths.DataDirectoryVariable, directory);
    }

    [Fact]
    public void Backup_CreateLoadDeleteRoundtrip()
    {
        Assert.False(DockCleanup.HasBackup());
        Assert.Null(DockCleanup.LoadBackup());

        var category = DockCleanupTests.Category("Kat", 0);
        var items = new List<DockItem>
        {
            category,
            DockCleanupTests.Item("Editor", @"C:\Tools\editor.exe", 1, category.Id)
        };

        DockCleanup.CreateBackup(items);

        Assert.True(DockCleanup.HasBackup());
        Assert.True(File.Exists(Path.Combine(directory, DockCleanup.BackupFileName)));
        Assert.Empty(Directory.GetFiles(directory, "*.tmp"));

        var loaded = DockCleanup.LoadBackup();
        Assert.NotNull(loaded);
        Assert.Equal(items.Select(item => (item.Id, item.DisplayName, item.FilePath, item.Category, item.IsCategory, item.Position)),
            loaded!.Select(item => (item.Id, item.DisplayName, item.FilePath, item.Category, item.IsCategory, item.Position)));

        DockCleanup.CreateBackup(items.Take(1).ToList());
        Assert.Single(DockCleanup.LoadBackup()!);

        DockCleanup.DeleteBackup();
        Assert.False(DockCleanup.HasBackup());
        Assert.Null(DockCleanup.LoadBackup());
        DockCleanup.DeleteBackup();
    }

    [Fact]
    public void LoadBackup_ReturnsNullForInvalidJson()
    {
        File.WriteAllText(Path.Combine(directory, DockCleanup.BackupFileName), "{ invalid json");

        Assert.Null(DockCleanup.LoadBackup());
        Assert.True(DockCleanup.HasBackup());
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(AppPaths.DataDirectoryVariable, previousDataDirectory);
        Directory.Delete(directory, recursive: true);
    }
}
