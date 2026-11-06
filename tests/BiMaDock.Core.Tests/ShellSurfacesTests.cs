using BiMaDock.Core.Windowing;

namespace BiMaDock.Core.Tests;

public class ShellSurfacesTests
{
    [Theory]
    [InlineData("StartMenuExperienceHost")]
    [InlineData("ShellExperienceHost")]
    [InlineData("SearchHost")]
    [InlineData("SearchApp")]
    [InlineData("ShellHost")]
    [InlineData("TextInputHost")]
    [InlineData("searchhost.exe")]
    [InlineData("StartMenuExperienceHost.EXE")]
    public void ShellProcesses_AreRecognized(string name) => Assert.True(ShellSurfaces.IsShellProcess(name));

    [Theory]
    [InlineData("mstsc")]
    [InlineData("vlc")]
    [InlineData("explorer")]
    [InlineData("SomeXamlGame")]
    [InlineData("")]
    [InlineData(null)]
    public void OtherProcesses_AreNotShell(string? name) => Assert.False(ShellSurfaces.IsShellProcess(name));

    [Theory]
    [InlineData("explorer", "XamlWindow", true)]
    [InlineData("explorer", "Windows.UI.Core.CoreWindow", true)]
    [InlineData("explorer", "CabinetWClass", false)]
    [InlineData("mstsc", "XamlWindow", false)]
    [InlineData("SomeXamlApp", "Windows.UI.Core.CoreWindow", false)]
    [InlineData("SearchHost", "Windows.UI.Core.CoreWindow", true)]
    [InlineData(null, "XamlWindow", false)]
    public void ShellSurface_NeedsProcessAndClass(string? process, string cls, bool expected) =>
        Assert.Equal(expected, ShellSurfaces.IsShellSurface(process, cls));
}
