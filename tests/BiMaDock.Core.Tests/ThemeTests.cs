using BiMaDock.Core.Settings;

namespace BiMaDock.Core.Tests;

public sealed class ThemeTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("bimadock-themes-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void ApplyTo_TakesLook_KeepsBehavior()
    {
        var current = new AppSettings { ShowDelayMs = 900, HideDelayMs = 1200, UpdateMode = UpdateMode.Off, DockColor = "#000000" };
        var theme = BuiltInThemes.All.Single(t => t.Name == "Neon");

        var result = theme.ApplyTo(current);

        Assert.Equal((900, 1200, UpdateMode.Off), (result.ShowDelayMs, result.HideDelayMs, result.UpdateMode));
        Assert.Equal("#0D0D12", result.DockColor);
        Assert.True(theme.Matches(result));
        Assert.False(theme.Matches(current));
    }

    [Fact]
    public void BuiltIns_HaveUniqueNames_AndAreNormalized()
    {
        Assert.Equal(BuiltInThemes.All.Count, BuiltInThemes.All.Select(t => t.Name).Distinct().Count());
        Assert.All(BuiltInThemes.All, t => Assert.Equal(t.Look.Normalized(), t.Look));
    }

    [Fact]
    public void Store_SavesLoadsReplacesAndDeletesUserThemes()
    {
        var store = new ThemeStore(_dir);
        var settings = new AppSettings { DockColor = "#123456", HoverEffect = HoverEffect.Pulse, ShowDelayMs = 999 };

        store.Save(DockTheme.FromSettings("Mein: Theme", settings));
        store.Save(DockTheme.FromSettings("Mein: Theme", settings with { DockColor = "#654321" }));

        var loaded = Assert.Single(store.LoadUserThemes());
        Assert.Equal(("Mein: Theme", "#654321", HoverEffect.Pulse, false), (loaded.Name, loaded.Look.DockColor, loaded.Look.HoverEffect, loaded.IsBuiltIn));
        Assert.Equal(new AppSettings().ShowDelayMs, loaded.Look.ShowDelayMs);
        Assert.Equal(BuiltInThemes.All.Count + 1, store.LoadAll().Count);

        store.Delete("Mein: Theme");
        Assert.Empty(store.LoadUserThemes());
    }

    [Fact]
    public void Store_SkipsBrokenFiles()
    {
        File.WriteAllText(Path.Combine(_dir, "kaputt.json"), "{ nicht json");
        Assert.Empty(new ThemeStore(_dir).LoadUserThemes());
    }

    [Fact]
    public void IsBuiltInName_IgnoresCaseAndSpaces() => Assert.True(ThemeStore.IsBuiltInName("  neon "));
}
