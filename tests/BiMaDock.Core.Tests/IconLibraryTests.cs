using BiMaDock.Core.Icons;

namespace BiMaDock.Core.Tests;

public sealed class IconLibraryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bimadock-icons-" + Guid.NewGuid().ToString("N"));
    private readonly string _builtIn;
    private readonly string _user;

    public IconLibraryTests()
    {
        _builtIn = Path.Combine(_root, "app", "Icons");
        _user = Path.Combine(_root, "profile", "Icons");
        Directory.CreateDirectory(Path.Combine(_builtIn, "Medien"));
        File.WriteAllBytes(Path.Combine(_builtIn, "Medien", "Musik.png"), [1]);
        File.WriteAllBytes(Path.Combine(_builtIn, "Medien", "notes.txt"), [1]);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Load_finds_builtin_icons_and_ignores_other_files()
    {
        var icons = new IconLibrary(_builtIn, _user).Load();

        var icon = Assert.Single(icons);
        Assert.Equal("Musik", icon.Name);
        Assert.Equal("Medien", icon.Category);
        Assert.Equal("lib:Medien/Musik.png", icon.Reference);
        Assert.True(icon.IsBuiltIn);
    }

    [Fact]
    public void References_resolve_against_their_folder_and_plain_paths_pass_through()
    {
        var library = new IconLibrary(_builtIn, _user);

        Assert.Equal(Path.Combine(_builtIn, "Medien", "Musik.png"), library.Resolve("lib:Medien/Musik.png"));
        Assert.Equal(Path.Combine(_user, "Eigene", "x.png"), library.Resolve("user:Eigene/x.png"));
        Assert.Equal(@"C:\bild.png", library.Resolve(@"C:\bild.png"));
        Assert.Null(library.Resolve(null));
    }

    [Fact]
    public void Resolve_blocks_escaping_the_library_folder()
    {
        Assert.Null(new IconLibrary(_builtIn, _user).Resolve("lib:../../geheim.png"));
    }

    [Fact]
    public void Import_copies_into_user_folder_numbers_duplicates_and_delete_cleans_up()
    {
        var library = new IconLibrary(_builtIn, _user);
        string source = Path.Combine(_root, "logo.png");
        File.WriteAllBytes(source, [1, 2]);

        var first = library.Import(source);
        var second = library.Import(source);

        Assert.Equal("user:Eigene/logo.png", first.Reference);
        Assert.Equal("logo 2", second.Name);
        Assert.Equal(3, library.Load().Count);

        library.Delete(first);
        library.Delete(second);
        Assert.False(Directory.Exists(Path.Combine(_user, "Eigene")));
        Assert.Throws<InvalidOperationException>(() => library.Delete(library.Load()[0]));
    }

    [Fact]
    public void Import_rejects_unsupported_files()
    {
        string source = Path.Combine(_root, "virus.exe");
        File.WriteAllBytes(source, [1]);

        Assert.Throws<NotSupportedException>(() => new IconLibrary(_builtIn, _user).Import(source));
    }
}

public sealed class AnimationSettingsTests
{
    [Fact]
    public void Animation_values_are_clamped_to_sensible_ranges()
    {
        var s = new BiMaDock.Core.Settings.AppSettings
        {
            HoverTunings = new() { [BiMaDock.Core.Settings.HoverEffect.Hop] = new BiMaDock.Core.Settings.HoverTuning { Intensity = 999, Speed = 1 } }, LaunchBounceCount = 9, SlideSpring = 0, LaunchBounceHeight = 100, MagnifyScale = 50,
            MagnifyRange = 9, SlideDurationMs = 5, ItemPopDurationMs = 5000,
            SlideStyle = (BiMaDock.Core.Settings.SlideStyle)42,
        }.Normalized();

        Assert.Equal(200, s.Hover(BiMaDock.Core.Settings.HoverEffect.Hop).Intensity);
        Assert.Equal(50, s.Hover(BiMaDock.Core.Settings.HoverEffect.Hop).Speed);
        Assert.Equal(100, s.Hover(BiMaDock.Core.Settings.HoverEffect.Pulse).Intensity);
        Assert.Equal(4, s.LaunchBounceCount);
        Assert.Equal(5, s.SlideSpring);
        Assert.Equal(30, s.LaunchBounceHeight);
        Assert.Equal(110, s.MagnifyScale);
        Assert.Equal(4, s.MagnifyRange);
        Assert.Equal(80, s.SlideDurationMs);
        Assert.Equal(600, s.ItemPopDurationMs);
        Assert.Equal(BiMaDock.Core.Settings.SlideStyle.Spring, s.SlideStyle);
    }

    [Fact]
    public void Each_hover_effect_keeps_its_own_values()
    {
        string file = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var store = new BiMaDock.Core.Settings.SettingsStore(file);
            store.Save(new BiMaDock.Core.Settings.AppSettings
            {
                HoverTunings = new()
                {
                    [BiMaDock.Core.Settings.HoverEffect.Hop] = new() { Intensity = 150, Speed = 80 },
                    [BiMaDock.Core.Settings.HoverEffect.Swing] = new() { Intensity = 60, Speed = 180 },
                },
            });
            var loaded = store.Load();
            Assert.Equal(150, loaded.Hover(BiMaDock.Core.Settings.HoverEffect.Hop).Intensity);
            Assert.Equal(180, loaded.Hover(BiMaDock.Core.Settings.HoverEffect.Swing).Speed);
            Assert.Equal(100, loaded.Hover(BiMaDock.Core.Settings.HoverEffect.Wiggle).Speed);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Enums_are_saved_as_text()
    {
        string file = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var store = new BiMaDock.Core.Settings.SettingsStore(file);
            store.Save(new BiMaDock.Core.Settings.AppSettings { HoverEffect = BiMaDock.Core.Settings.HoverEffect.Swing });
            Assert.Contains("\"Swing\"", File.ReadAllText(file));
            Assert.Equal(BiMaDock.Core.Settings.HoverEffect.Swing, store.Load().HoverEffect);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
