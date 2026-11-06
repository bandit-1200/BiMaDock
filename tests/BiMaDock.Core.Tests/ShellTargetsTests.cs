using BiMaDock.Core.Cleanup;
using BiMaDock.Core.Model;

namespace BiMaDock.Core.Tests;

public class ShellTargetsTests
{
    private static string? NoFolder(Guid _) => null;

    [Fact]
    public void Classify_PrefersFileSystemPath()
    {
        string? target = ShellTargets.Classify(@"C:\Menu\Excel.lnk", "Microsoft.Office.EXCEL.EXE.15", NoFolder, _ => false);

        Assert.Equal(@"C:\Menu\Excel.lnk", target);
    }

    [Theory]
    [InlineData("Microsoft.WindowsCalculator_8wekyb3d8bbwe!App")]
    [InlineData("MSEdge")]
    public void Classify_StoreAppBecomesAppsFolderTarget(string aumid)
    {
        Assert.Equal(@"shell:AppsFolder\" + aumid, ShellTargets.Classify(null, aumid, NoFolder, _ => false));
    }

    [Fact]
    public void Classify_KnownFolderPathBecomesRealPathWhenFileExists()
    {
        var id = Guid.NewGuid();
        string name = $@"{{{id}}}\Notepad++\notepad++.exe";

        string? found = ShellTargets.Classify(null, name, g => g == id ? @"C:\Program Files" : null, _ => true);
        string? missing = ShellTargets.Classify(null, name, g => g == id ? @"C:\Program Files" : null, _ => false);

        Assert.Equal(@"C:\Program Files\Notepad++\notepad++.exe", found);
        Assert.Equal(@"shell:AppsFolder\" + name, missing);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData(null, "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}")]
    public void Classify_ReturnsNullWithoutUsableData(string? path, string? parsing) =>
        Assert.Null(ShellTargets.Classify(path, parsing, NoFolder, _ => false));

    [Theory]
    [InlineData(@"shell:AppsFolder\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", "Microsoft.WindowsCalculator")]
    [InlineData(@"shell:AppsFolder\{6D809377-6AF0-444B-8957-A3773F02200E}\Notepad++\notepad++.exe", "notepad++")]
    public void Factory_AcceptsAppsFolderTargets(string value, string expectedName)
    {
        var entry = DockEntryFactory.FromDropValue(value);

        Assert.NotNull(entry);
        Assert.Equal(value, entry.Target);
        Assert.Equal(expectedName, entry.Name);
    }

    [Fact]
    public void FromShellItem_UsesDisplayName()
    {
        var entry = DockEntryFactory.FromShellItem(@"shell:AppsFolder\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", " Rechner ");

        Assert.Equal("Rechner", entry!.Name);
        Assert.Equal(DockEntryKind.Launch, entry.Kind);
    }

    [Fact]
    public void AppsFolderTarget_IsNotMissingAndTakesNoFiles()
    {
        var entry = DockEntryFactory.FromDropValue(@"shell:AppsFolder\{6D809377-6AF0-444B-8957-A3773F02200E}\Notepad++\notepad++.exe")!;

        Assert.Equal(PathState.Exists, CleanupScanner.ProbePath(entry.Target));
        Assert.False(LaunchArguments.AcceptsFiles(entry));
    }

    [Fact]
    public void IdListArray_ParsesOffsets()
    {
        var data = new byte[40];
        BitConverter.GetBytes(2u).CopyTo(data, 0);
        BitConverter.GetBytes(16u).CopyTo(data, 4);
        BitConverter.GetBytes(20u).CopyTo(data, 8);
        BitConverter.GetBytes(30u).CopyTo(data, 12);

        var offsets = ShellIdListArray.ParseOffsets(data);

        Assert.NotNull(offsets);
        Assert.Equal(16, offsets.Value.Parent);
        Assert.Equal([20, 30], offsets.Value.Children);
    }

    [Fact]
    public void IdListArray_RejectsBrokenData()
    {
        var data = new byte[12];
        BitConverter.GetBytes(5u).CopyTo(data, 0);

        Assert.Null(ShellIdListArray.ParseOffsets(data));
        Assert.Null(ShellIdListArray.ParseOffsets([]));
    }
}
