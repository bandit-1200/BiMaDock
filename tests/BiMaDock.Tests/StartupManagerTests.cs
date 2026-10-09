using Xunit;

namespace BiMaDock.Tests;

public class StartupManagerTests
{
    [Theory]
    [InlineData(@"C:\Program Files\BiMaDock\BiMaDock.exe", "\"C:\\Program Files\\BiMaDock\\BiMaDock.exe\"")]
    [InlineData(@"C:\BiMaDock\BiMaDock.exe", "\"C:\\BiMaDock\\BiMaDock.exe\"")]
    public void BuildStartupCommand_QuotesExecutablePath(string executablePath, string expected)
    {
        Assert.Equal(expected, StartupManager.BuildStartupCommand(executablePath));
    }
}
