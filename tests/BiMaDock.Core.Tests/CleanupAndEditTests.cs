using BiMaDock.Core.Cleanup;
using BiMaDock.Core.Model;
using BiMaDock.Core.Settings;

namespace BiMaDock.Core.Tests;

public sealed class CleanupAndEditTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("bimadock-cleanup-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Scan_PreselectsMissingOnly_AndFindsCategoriesThatWouldBeEmpty()
    {
        var full = new DockEntry { Name = "Voll", Kind = DockEntryKind.Category };
        var empty = new DockEntry { Name = "Leer", Kind = DockEntryKind.Category };
        var doomed = new DockEntry { Name = "Bald leer", Kind = DockEntryKind.Category };
        var layout = new DockLayout
        {
            Entries =
            [
                full, empty, doomed,
                new DockEntry { Name = "ok", Target = "ok", CategoryId = full.Id },
                new DockEntry { Name = "weg", Target = "weg", CategoryId = doomed.Id },
                new DockEntry { Name = "nas", Target = "nas" },
                new DockEntry { Name = "web", Target = "https://example.org" },
            ],
        };

        var result = CleanupScanner.Scan(layout, path => path switch
        {
            "ok" => PathState.Exists,
            "nas" => PathState.Unreachable,
            _ => PathState.Missing,
        });

        Assert.Equal(
            ["weg:Missing:True", "nas:Unreachable:False", "Leer:EmptyCategory:False", "Bald leer:EmptyCategory:False"],
            result.Select(c => $"{c.Entry.Name}:{c.Reason}:{c.SelectedByDefault}"));
    }

    [Fact]
    public void ProbePath_DetectsExistingAndMissingLocalPaths()
    {
        string file = Path.Combine(_dir, "a.txt");
        File.WriteAllText(file, "x");

        Assert.Equal(PathState.Exists, CleanupScanner.ProbePath(file));
        Assert.Equal(PathState.Exists, CleanupScanner.ProbePath(_dir));
        Assert.Equal(PathState.Missing, CleanupScanner.ProbePath(Path.Combine(_dir, "fehlt.exe")));
        Assert.Equal(PathState.Unreachable, CleanupScanner.ProbePath(@"\\gibtsnicht-bimadock\freigabe\x.exe"));
    }

    [Fact]
    public void Remove_Category_RemovesItsContent()
    {
        var category = new DockEntry { Name = "K", Kind = DockEntryKind.Category };
        var layout = new DockLayout { Entries = [category, new DockEntry { Name = "a", CategoryId = category.Id }, new DockEntry { Name = "b" }] };

        Assert.Equal(2, layout.Remove(category.Id));
        Assert.Equal("b", Assert.Single(layout.Entries).Name);
        Assert.Equal(0, layout.Remove(Guid.NewGuid()));
    }

    [Fact]
    public void Update_KeepsKindAndCategory_AndIgnoresEmptyName()
    {
        var category = new DockEntry { Name = "K", Kind = DockEntryKind.Category };
        var item = new DockEntry { Name = "alt", Target = @"C:\a.exe", CategoryId = category.Id };
        var layout = new DockLayout { Entries = [category, item] };

        Assert.True(layout.Update(item with { Name = "  ", Target = @" C:\b.exe ", IconPath = " ", CategoryId = null, Kind = DockEntryKind.Category }));

        var updated = layout.Find(item.Id)!;
        Assert.Equal("alt", updated.Name);
        Assert.Equal(@"C:\b.exe", updated.Target);
        Assert.Null(updated.IconPath);
        Assert.Equal(category.Id, updated.CategoryId);
        Assert.Equal(DockEntryKind.Launch, updated.Kind);
    }

    [Fact]
    public void Clone_IsIndependentOfOriginal()
    {
        var layout = new DockLayout { Entries = [new DockEntry { Name = "a" }] };
        var copy = layout.Clone();

        layout.Entries.Clear();

        Assert.Single(copy.Entries);
    }

    [Fact]
    public void Settings_RoundTripAndClampOutOfRangeValues()
    {
        var store = new SettingsStore(Path.Combine(_dir, "settings.json"));
        Assert.Equal(new AppSettings(), store.Load());

        store.Save(new AppSettings { ShowDelayMs = 99999, HideDelayMs = -5, FlowAnimationEnabled = false, FlowDurationMs = 10 });
        var loaded = store.Load();

        Assert.Equal(AppSettings.MaxDelay, loaded.ShowDelayMs);
        Assert.Equal(0, loaded.HideDelayMs);
        Assert.False(loaded.FlowAnimationEnabled);
        Assert.Equal(AppSettings.MinFlow, loaded.FlowDurationMs);
    }

    [Theory]
    [InlineData("#2a2a30", "#2A2A30")]
    [InlineData("  ff8800 ", "#FF8800")]
    [InlineData("#abc", "#AABBCC")]
    [InlineData("#12345", "#FALLBK")]
    [InlineData("grün", "#FALLBK")]
    [InlineData(null, "#FALLBK")]
    public void NormalizeColor_AcceptsHexAndRejectsGarbage(string? input, string expected) =>
        Assert.Equal(expected, AppSettings.NormalizeColor(input, "#FALLBK"));

    [Fact]
    public void Settings_ColorsAreValidatedOnLoad()
    {
        var store = new SettingsStore(Path.Combine(_dir, "settings.json"));
        store.Save(new AppSettings { DockColor = "kaputt", AccentColor = "#00ff00", DockOpacity = 5, FeedbackColor = "" });

        var loaded = store.Load();

        Assert.Equal(AppSettings.DefaultDockColor, loaded.DockColor);
        Assert.Equal("#00FF00", loaded.AccentColor);
        Assert.Equal(AppSettings.MinOpacity, loaded.DockOpacity);
        Assert.Equal(AppSettings.DefaultFeedbackColor, loaded.FeedbackColor);
    }

    [Fact]
    public void Settings_CorruptFile_FallsBackToDefaults()
    {
        string file = Path.Combine(_dir, "settings.json");
        File.WriteAllText(file, "{ kaputt");

        Assert.Equal(new AppSettings(), new SettingsStore(file).Load());
    }
}
