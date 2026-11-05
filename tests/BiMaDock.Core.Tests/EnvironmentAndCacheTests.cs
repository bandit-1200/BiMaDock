using BiMaDock.Core.Launching;
using BiMaDock.Core.Icons;
using BiMaDock.Core.Model;

namespace BiMaDock.Core.Tests;

public sealed class EnvironmentAndCacheTests
{
    [Fact]
    public void Merge_UserOverridesSystem_PathIsAppended_VolatileWins()
    {
        var machine = new Dictionary<string, string> { ["Path"] = @"C:\Windows;", ["TEMP"] = @"C:\Temp", ["OS"] = "Windows_NT" };
        var user = new Dictionary<string, string> { ["PATH"] = @"C:\Tools", ["TEMP"] = @"C:\Users\m\Temp" };
        var volatileValues = new Dictionary<string, string> { ["OS"] = "x" };

        var result = EnvironmentMerger.Merge(machine, user, volatileValues);

        Assert.Equal(@"C:\Windows;C:\Tools", result["path"]);
        Assert.Equal(@"C:\Users\m\Temp", result["TEMP"]);
        Assert.Equal("x", result["OS"]);
    }

    [Fact]
    public void CacheKey_ChangesWithTargetDate_IgnoresCase()
    {
        var entry = new DockEntry { Name = "a", Target = @"C:\App\a.exe" };
        var date = new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc);

        string key = IconCacheKey.FileName(entry, date, 128);

        Assert.Equal(key, IconCacheKey.FileName(entry with { Target = @"c:\app\A.EXE", Name = "anders" }, date, 128));
        Assert.NotEqual(key, IconCacheKey.FileName(entry, date.AddDays(1), 128));
        Assert.EndsWith(".png", key, StringComparison.Ordinal);
    }
}
