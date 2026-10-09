using System.IO;
using System.Windows;
using Xunit;

namespace BiMaDock.Tests;

public class DockDropLinkTests
{
    [Theory]
    [InlineData("http://example.com", "http://example.com")]
    [InlineData("https://example.com/path?q=1", "https://example.com/path?q=1")]
    [InlineData("file:///C:/Temp/item.txt", "file:///C:/Temp/item.txt")]
    [InlineData("   https://example.com  ", "https://example.com")]
    [InlineData("https://example.com\r\nzweite Zeile", "https://example.com")]
    [InlineData("\n  https://example.com/first\nhttps://example.com/second", "https://example.com/first")]
    public void TryGetLinkText_AcceptsAbsoluteHttpHttpsAndFileLinks(string text, string expected)
    {
        var data = new DataObject(DataFormats.UnicodeText, text);

        Assert.True(DockDropPosition.TryGetLinkText(data, out string link));
        Assert.Equal(expected, link);
    }

    [Theory]
    [InlineData("einfacher Text")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("example.com")]
    [InlineData("ftp://example.com/file")]
    [InlineData("javascript:alert(1)")]
    public void TryGetLinkText_RejectsInvalidText(string text)
    {
        var data = new DataObject(DataFormats.UnicodeText, text);

        Assert.False(DockDropPosition.TryGetLinkText(data, out string link));
        Assert.Equal(string.Empty, link);
    }

    [Fact]
    public void TryGetLinkText_ReadsAnsiTextFormat()
    {
        var data = new DataObject(DataFormats.Text, "https://example.com");

        Assert.True(DockDropPosition.TryGetLinkText(data, out string link));
        Assert.Equal("https://example.com", link);
    }

    [Fact]
    public void TryGetLinkText_PrefersUrlFormatOverText()
    {
        var data = new DataObject();
        data.SetData(DataFormats.UnicodeText, "Seitentitel");
        data.SetData("UniformResourceLocatorW", "https://example.com/page");

        Assert.True(DockDropPosition.TryGetLinkText(data, out string link));
        Assert.Equal("https://example.com/page", link);
    }

    [Fact]
    public void TryGetLinkText_ReadsUrlFormatFromStream()
    {
        var data = new DataObject();
        byte[] bytes = System.Text.Encoding.Unicode.GetBytes("https://example.com/stream\0");
        data.SetData("UniformResourceLocatorW", new MemoryStream(bytes));

        Assert.True(DockDropPosition.TryGetLinkText(data, out string link));
        Assert.Equal("https://example.com/stream", link);
    }

    [Fact]
    public void TryGetLinkText_RejectsDataWithoutText()
    {
        var data = new DataObject("Unsupported format", "https://example.com");

        Assert.False(DockDropPosition.TryGetLinkText(data, out string link));
        Assert.Equal(string.Empty, link);
    }

    [Theory]
    [InlineData(@"C:\Temp\Notizen.txt", "Notizen")]
    [InlineData(@"C:\Users\Public\Desktop\Editor.lnk", "Editor")]
    [InlineData(@"C:\", "C:")]
    [InlineData(@"d:\", "D:")]
    [InlineData(@"C:\Temp\Ordner\", "Ordner")]
    [InlineData("https://www.example.com/path", "example.com")]
    [InlineData("http://example.org", "example.org")]
    public void GetDisplayName_ReturnsReadableName(string input, string expected)
    {
        Assert.Equal(expected, DockDropPosition.GetDisplayName(input));
    }

    [Fact]
    public void GetDisplayName_UsesDirectoryNameForExistingDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "BiMaDock.Test.Ordner." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            Assert.Equal(Path.GetFileName(directory), DockDropPosition.GetDisplayName(directory));
        }
        finally
        {
            Directory.Delete(directory);
        }
    }

    [Fact]
    public void GetDisplayName_NeverReturnsEmpty()
    {
        Assert.Equal("irgendwas", DockDropPosition.GetDisplayName("irgendwas"));
        Assert.False(string.IsNullOrEmpty(DockDropPosition.GetDisplayName(@"\\")));
    }

    [Fact]
    public void IsSupportedDrop_RecognizesSupportedFormats()
    {
        Assert.True(DockDropPosition.IsSupportedDrop(new DataObject(DockDropPosition.DockItemButtonFormat, new object())));
        Assert.True(DockDropPosition.IsSupportedDrop(new DataObject(DataFormats.FileDrop, new[] { @"C:\Temp\item.txt" })));
        Assert.True(DockDropPosition.IsSupportedDrop(new DataObject("FileNameW", @"C:\Temp\item.txt")));
        Assert.True(DockDropPosition.IsSupportedDrop(new DataObject(DataFormats.UnicodeText, "https://example.com")));
        Assert.False(DockDropPosition.IsSupportedDrop(new DataObject(DataFormats.UnicodeText, "kein Link")));
        Assert.False(DockDropPosition.IsSupportedDrop(new DataObject("Unsupported format", "data")));
    }
}
