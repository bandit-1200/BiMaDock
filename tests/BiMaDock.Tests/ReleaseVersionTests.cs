using BiMaDock;
using Xunit;

namespace BiMaDock.Tests;

public class ReleaseVersionTests
{
    [Theory]
    [InlineData("V26.10.07-Build3", 26, 10, 7, 3)]
    [InlineData("v25.11.2-Build1", 25, 11, 2, 1)]
    [InlineData("26.10.0.4+a30540902a", 26, 10, 0, 4)]
    [InlineData("v24.12.8", 24, 12, 8, 0)]
    public void TryParse_ParsesReleaseAndInstalledVersions(
        string input,
        int major,
        int minor,
        int build,
        int revision)
    {
        Assert.True(ReleaseVersion.TryParse(input, out Version parsed));
        Assert.Equal(new Version(major, minor, build, revision), parsed);
    }

    [Theory]
    [InlineData("V26.10.07-Build2", "26.10.0.4+a30540902a", true)]
    [InlineData("V26.10.07-Build3", "V26.10.07-Build2", true)]
    [InlineData("V26.10.08-Build1", "V26.10.07-Build3", true)]
    [InlineData("V26.10.07-Build3", "V26.10.07-Build3", false)]
    [InlineData("V26.10.07-Build2", "V26.10.07-Build3", false)]
    public void IsNewer_ComparesDateAndBuild(
        string candidateVersion,
        string currentVersion,
        bool expected)
    {
        Assert.Equal(expected, ReleaseVersion.IsNewer(currentVersion, candidateVersion));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Unknown")]
    [InlineData("V26.10.07-Build999999999999999999999")]
    [InlineData("V26.10.07-Preview1")]
    public void TryParse_RejectsInvalidVersions(string input)
    {
        Assert.False(ReleaseVersion.TryParse(input, out _));
    }

    [Fact]
    public void DeferredRelease_DoesNotHideANewerBuild()
    {
        string[] settings = { "V26.10.07-Build2", "2026-10-08T00:00:00.0000000+00:00" };

        Assert.True(UpdateChecker.IsDeferredForRelease(
            settings,
            "V26.10.07-Build2",
            new DateTimeOffset(2026, 10, 7, 20, 0, 0, TimeSpan.Zero)));
        Assert.False(UpdateChecker.IsDeferredForRelease(
            settings,
            "V26.10.07-Build3",
            new DateTimeOffset(2026, 10, 7, 20, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void DeferredRelease_IgnoresLegacyDateOnlySetting()
    {
        string[] legacySettings = { "2026-10-08T00:00:00.0000000+00:00" };

        Assert.False(UpdateChecker.IsDeferredForRelease(
            legacySettings,
            "V26.10.07-Build3",
            new DateTimeOffset(2026, 10, 7, 20, 0, 0, TimeSpan.Zero)));
    }
}
