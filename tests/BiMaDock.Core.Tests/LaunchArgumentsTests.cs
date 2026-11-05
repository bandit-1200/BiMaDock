using BiMaDock.Core.Model;
using BiMaDock.Core.Storage;

namespace BiMaDock.Core.Tests;

public sealed class LaunchArgumentsTests
{
    [Fact]
    public void Build_CombinesOwnArgumentsAndQuotedFiles()
    {
        string result = LaunchArguments.Build(" -n ", [@"C:\Mit Leer\a.txt", "\"b.txt\""]);

        Assert.Equal(@"-n ""C:\Mit Leer\a.txt"" ""b.txt""", result);
    }

    [Fact]
    public void Build_EmptyWithoutAnything() => Assert.Equal(string.Empty, LaunchArguments.Build(null));

    [Theory]
    [InlineData(@"C:\Programme\npp.exe", true)]
    [InlineData(@"C:\Desktop\Excel.LNK", true)]
    [InlineData(@"C:\Daten\liste.txt", false)]
    [InlineData(@"C:\Daten", false)]
    [InlineData("https://example.org", false)]
    public void AcceptsFiles_OnlyProgramsAndShortcuts(string target, bool expected) =>
        Assert.Equal(expected, LaunchArguments.AcceptsFiles(new DockEntry { Name = "x", Target = target }));

    [Fact]
    public void Store_RoundTripsLaunchOptions()
    {
        string dir = Directory.CreateTempSubdirectory("bimadock-args-").FullName;
        try
        {
            var store = new DockStore(Path.Combine(dir, "dock.json"));
            var entry = new DockEntry { Name = "RDP", Target = "mstsc.exe", Arguments = "/v:server", WorkingDirectory = @"C:\Temp", RunAsAdmin = true };
            store.Save(new DockLayout { Entries = [entry] });

            var loaded = Assert.Single(store.Load().Entries);
            Assert.Equal(("/v:server", @"C:\Temp", true), (loaded.Arguments, loaded.WorkingDirectory, loaded.RunAsAdmin));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
