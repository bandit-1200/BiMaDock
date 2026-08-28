using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace BiMaDock;

/// <summary>
/// Richtet eine Kategoriebar am auslösenden Hauptdock-Element aus und hält sie im sichtbaren Bereich.
/// </summary>
internal static class CategoryDockPositioner
{
    public static void Position(
        string categoryId,
        Panel mainDockItems,
        FrameworkElement dockHost,
        FrameworkElement mainDockSurface,
        Border categoryDock,
        Line connectorLine)
    {
        var categoryButton = mainDockItems.Children.OfType<Button>()
            .FirstOrDefault(button => button.Tag is DockItem item && item.Id == categoryId);

        if (categoryButton == null || categoryDock.ActualWidth <= 0 || dockHost.ActualWidth <= 0)
        {
            return;
        }

        double buttonCenterX = categoryButton.TranslatePoint(
            new Point(categoryButton.ActualWidth / 2, 0), dockHost).X;
        double maximumLeft = Math.Max(0, dockHost.ActualWidth - categoryDock.ActualWidth);
        double left = Math.Clamp(buttonCenterX - (categoryDock.ActualWidth / 2), 0, maximumLeft);
        categoryDock.Margin = new Thickness(left, 0, 0, 0);

        double centerOnSurface = categoryButton.TranslatePoint(
            new Point(categoryButton.ActualWidth / 2, 0), mainDockSurface).X;
        Canvas.SetLeft(connectorLine,
            centerOnSurface - ((connectorLine.X2 - connectorLine.X1) / 2) - connectorLine.X1);
        Canvas.SetTop(connectorLine, 80);
    }
}
