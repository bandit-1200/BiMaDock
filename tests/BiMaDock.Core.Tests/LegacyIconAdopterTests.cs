using BiMaDock.Core.Icons;
using BiMaDock.Core.Model;

namespace BiMaDock.Core.Tests;

public sealed class LegacyIconAdopterTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bimadock-adopt-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Old_icons_are_copied_into_user_library_and_survive_deleting_the_old_folder()
    {
        string oldDir = Path.Combine(_root, "BiMaDock", "Icons");
        Directory.CreateDirectory(oldDir);
        string oldIcon = Path.Combine(oldDir, "firewall.png");
        File.WriteAllBytes(oldIcon, [1, 2, 3]);
        string otherIcon = Path.Combine(_root, "Desktop", "eigen.png");
        Directory.CreateDirectory(Path.GetDirectoryName(otherIcon)!);
        File.WriteAllBytes(otherIcon, [4]);

        var library = new IconLibrary(Path.Combine(_root, "lib"), Path.Combine(_root, "BiMaDock2", "Icons"));
        var layout = new DockLayout
        {
            Entries =
            [
                new DockEntry { Name = "A", IconPath = oldIcon.Replace('\\', '/') },
                new DockEntry { Name = "B", IconPath = oldIcon },
                new DockEntry { Name = "C", IconPath = otherIcon },
                new DockEntry { Name = "D", IconPath = Path.Combine(oldDir, "fehlt.png") },
            ],
        };

        Assert.Equal(2, LegacyIconAdopter.Adopt(layout, library));
        Assert.Equal("user:Altes Dock/firewall.png", layout.Entries[0].IconPath);
        Assert.Equal(layout.Entries[0].IconPath, layout.Entries[1].IconPath);
        Assert.Equal(otherIcon, layout.Entries[2].IconPath);

        Directory.Delete(Path.Combine(_root, "BiMaDock"), recursive: true);
        Assert.True(File.Exists(library.Resolve(layout.Entries[0].IconPath)));

        // Zweiter Lauf ändert nichts und legt keine Kopien an.
        Assert.Equal(0, LegacyIconAdopter.Adopt(layout, library));
        Assert.Single(library.Load());
    }
}
