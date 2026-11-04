using BiMaDock.Core.Updates;

namespace BiMaDock.Core.Tests;

public sealed class UpdateCheckerTests
{
    private const string Release = """
        {
          "tag_name": "v2.1.0", "draft": false, "prerelease": false,
          "html_url": "https://github.com/bandit-1200/BiMaDock/releases/tag/v2.1.0",
          "body": "Neu: Updates",
          "assets": [
            { "name": "Quellcode.zip", "size": 5, "browser_download_url": "https://x/zip" },
            { "name": "BiMaDock2-Setup-2.1.0.exe", "size": 4711, "browser_download_url": "https://x/setup.exe" }
          ]
        }
        """;

    [Fact]
    public void Parse_reads_version_and_installer_asset()
    {
        var info = UpdateChecker.Parse(Release);
        Assert.NotNull(info);
        Assert.Equal(new Version(2, 1, 0), info.Version);
        Assert.Equal("https://x/setup.exe", info.DownloadUrl);
        Assert.Equal(4711, info.Size);
        Assert.Equal("Neu: Updates", info.Notes);
    }

    [Fact]
    public void Parse_ignores_releases_without_installer_and_prereleases()
    {
        Assert.Null(UpdateChecker.Parse("""{ "tag_name": "v2.1.0", "assets": [] }"""));
        Assert.Null(UpdateChecker.Parse(Release.Replace("\"prerelease\": false", "\"prerelease\": true")));
        // Alte Releases der ersten Version (Tag 26.10.18, Anhang BiMaDockSetup.exe) werden nicht angeboten.
        Assert.Null(UpdateChecker.Parse("""{ "tag_name": "26.10.18", "assets": [ { "name": "BiMaDockSetup.exe", "browser_download_url": "u" } ] }"""));
    }

    [Theory]
    [InlineData("v2.0.0", 2, 0, 0)]
    [InlineData("2.3", 2, 3, 0)]
    [InlineData("V10.1.2-beta", 10, 1, 2)]
    public void TryParseVersion_accepts_common_tags(string tag, int major, int minor, int build)
    {
        Assert.True(UpdateChecker.TryParseVersion(tag, out var v));
        Assert.Equal(new Version(major, minor, build), v);
    }

    [Fact]
    public void Four_part_assembly_version_equals_three_part_tag()
    {
        Assert.Equal(UpdateChecker.Normalize(new Version(2, 0, 0, 0)), new Version(2, 0, 0));
    }
}
