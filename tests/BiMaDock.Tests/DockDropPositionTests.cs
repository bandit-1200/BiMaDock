using System.Windows;
using Xunit;

namespace BiMaDock.Tests;

public class DockDropPositionTests
{
    [Theory]
    [InlineData(-10, 0)]
    [InlineData(10, 1)]
    [InlineData(30, 2)]
    [InlineData(45, 2)]
    [InlineData(50, 3)]
    [InlineData(100, 3)]
    public void FindInsertionIndex_ReturnsPositionRelativeToItemCenters(double dropX, int expectedIndex)
    {
        double[] itemCenters = { 10, 30, 50 };

        Assert.Equal(expectedIndex, DockDropPosition.FindInsertionIndex(itemCenters, dropX));
    }

    [Fact]
    public void FindInsertionIndex_ReturnsZeroForAnEmptyDock()
    {
        Assert.Equal(0, DockDropPosition.FindInsertionIndex(Array.Empty<double>(), 25));
    }

    [Fact]
    public void GetDropEffect_UsesMoveForDockItemsAndCopyForExternalItems()
    {
        var dockItemData = new DataObject(DockDropPosition.DockItemButtonFormat, new object());
        var fileData = new DataObject(DataFormats.FileDrop, new[] { @"C:\Temp\item.txt" });
        var shellFileNameData = new DataObject("FileNameW", @"C:\Temp\shell-item.txt");
        var textData = new DataObject(DataFormats.UnicodeText, "https://example.com");
        var unsupportedData = new DataObject("Unsupported format", "data");

        Assert.Equal(DragDropEffects.Move, MainWindow.GetDropEffect(dockItemData));
        Assert.Equal(DragDropEffects.Copy, MainWindow.GetDropEffect(fileData));
        Assert.Equal(DragDropEffects.Copy, MainWindow.GetDropEffect(shellFileNameData));
        Assert.Equal(DragDropEffects.Copy, MainWindow.GetDropEffect(textData));
        Assert.Equal(DragDropEffects.None, MainWindow.GetDropEffect(unsupportedData));
        Assert.Equal(
            new[] { @"C:\Temp\shell-item.txt" },
            DockDropPosition.GetDroppedFilePaths(shellFileNameData));
    }
}
