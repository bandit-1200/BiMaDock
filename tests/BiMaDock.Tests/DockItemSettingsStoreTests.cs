using System;
using System.IO;
using Xunit;

namespace BiMaDock.Tests;

public sealed class DockItemSettingsStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"BiMaDock.Tests.{Guid.NewGuid():N}");

    public DockItemSettingsStoreTests()
    {
        Directory.CreateDirectory(directory);
    }

    [Fact]
    public void Save_ReplacesSettingsWithoutLeavingTemporaryFiles()
    {
        var path = Path.Combine(directory, "docksettings.json");

        DockItemSettingsStore.Save(path, "[]");
        DockItemSettingsStore.Save(path, "[{}]");

        Assert.Equal("[{}]", File.ReadAllText(path));
        Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
    }

    [Fact]
    public void Load_DeserializesSavedDockItems()
    {
        var path = Path.Combine(directory, "docksettings.json");
        DockItemSettingsStore.Save(path, "[{\"DisplayName\":\"Editor\",\"FilePath\":\"C:\\\\Tools\\\\editor.exe\"}]");

        var items = DockItemSettingsStore.Load(path);

        var item = Assert.Single(items);
        Assert.Equal("Editor", item.DisplayName);
        Assert.Equal(@"C:\Tools\editor.exe", item.FilePath);
    }

    [Fact]
    public void Load_PreservesCorruptSettingsAndReturnsEmptyItems()
    {
        var path = Path.Combine(directory, "docksettings.json");
        File.WriteAllText(path, "{ invalid json");

        var items = DockItemSettingsStore.Load(path);

        Assert.Empty(items);
        Assert.False(File.Exists(path));
        var backupFiles = Directory.GetFiles(directory, "docksettings.json.corrupt-*");
        Assert.Single(backupFiles);
        Assert.Equal("{ invalid json", File.ReadAllText(backupFiles[0]));
    }

    public void Dispose()
    {
        Directory.Delete(directory, recursive: true);
    }
}
