using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
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
    public void FindInsertionIndex_IgnoresDropGapsAndHiddenButtons()
    {
        RunOnSta(() =>
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var first = CreateDockButton();
            var hidden = CreateDockButton();
            var second = CreateDockButton();
            hidden.Visibility = Visibility.Collapsed;
            var gap = new Border { Width = 80, Tag = DockDropPosition.DropGapTag };

            // Layout: first 0..80 (Mitte 40), hidden 0 breit, Lücke 80..160, second 160..240 (Mitte 200)
            panel.Children.Add(first);
            panel.Children.Add(hidden);
            panel.Children.Add(gap);
            panel.Children.Add(second);
            panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            panel.Arrange(new Rect(panel.DesiredSize));

            Assert.True(DockDropPosition.IsDropGap(gap));
            Assert.False(DockDropPosition.IsInsertionTarget(hidden));
            Assert.Equal(0, DockDropPosition.FindItemInsertionIndex(panel, 20));
            Assert.Equal(1, DockDropPosition.FindItemInsertionIndex(panel, 120));
            Assert.Equal(1, DockDropPosition.FindItemInsertionIndex(panel, 190));
            Assert.Equal(2, DockDropPosition.FindItemInsertionIndex(panel, 210));

            Assert.Equal(panel.Children.IndexOf(first), DockDropPosition.FindInsertionIndex(panel, 20));
            Assert.Equal(panel.Children.IndexOf(second), DockDropPosition.FindInsertionIndex(panel, 120));
            Assert.Equal(panel.Children.Count, DockDropPosition.FindInsertionIndex(panel, 210));
        });
    }

    private static Button CreateDockButton()
    {
        return new Button { Width = 70, Margin = new Thickness(5) };
    }

    private static void RunOnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure != null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
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
