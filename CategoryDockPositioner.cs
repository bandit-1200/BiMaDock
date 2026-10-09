using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace BiMaDock;

/// <summary>
/// Richtet eine Kategoriebar unter dem auslösenden Hauptdock-Element aus und hält sie innerhalb
/// der Breite des Hauptdocks. Nur eine Kategoriebar, die breiter als das Hauptdock ist, steht
/// (mittig) beidseitig über.
/// </summary>
internal static class CategoryDockPositioner
{
    public static void Position(
        string categoryId,
        Panel mainDockItems,
        FrameworkElement mainDock,
        FrameworkElement mainDockSurface,
        Border categoryDock,
        Line connectorLine)
    {
        var categoryButton = FindCategoryButton(mainDockItems, categoryId);

        if (categoryButton == null || categoryDock.ActualWidth <= 0 || mainDock.ActualWidth <= 0)
        {
            return;
        }

        // Relativ zum Hauptdock rechnen: Der gemeinsame Container wächst mit der Kategoriebar mit
        // und taugt deshalb nicht als Begrenzung.
        double buttonCenterOnMainDock = categoryButton.TranslatePoint(
            new Point(categoryButton.ActualWidth / 2, 0), mainDock).X;
        double left = CalculateLeftMargin(buttonCenterOnMainDock, mainDock.ActualWidth, categoryDock.ActualWidth);
        categoryDock.Margin = new Thickness(left, 0, 0, 0);

        double centerOnSurface = categoryButton.TranslatePoint(
            new Point(categoryButton.ActualWidth / 2, 0), mainDockSurface).X;
        Canvas.SetLeft(connectorLine,
            centerOnSurface - ((connectorLine.X2 - connectorLine.X1) / 2) - connectorLine.X1);
        Canvas.SetTop(connectorLine, 80);
    }

    public static Button? FindCategoryButton(Panel mainDockItems, string categoryId) =>
        mainDockItems.Children.OfType<Button>()
            .FirstOrDefault(button => button.Tag is DockItem item && item.Id == categoryId);

    /// <summary>
    /// Linker Rand der Kategoriebar im gemeinsamen, zentrierten Container. Dieser ist so breit wie die
    /// breitere der beiden Leisten; das Hauptdock liegt darin mittig.
    /// </summary>
    /// <param name="buttonCenterOnMainDock">Mitte des Kategorie-Elements relativ zum linken Rand des Hauptdocks.</param>
    internal static double CalculateLeftMargin(double buttonCenterOnMainDock, double mainDockWidth, double categoryDockWidth)
    {
        double containerWidth = Math.Max(mainDockWidth, categoryDockWidth);
        double mainDockLeft = (containerWidth - mainDockWidth) / 2;

        if (categoryDockWidth >= mainDockWidth)
        {
            return 0; // Zu breit: mittig unter dem Hauptdock, steht beidseitig gleich weit über
        }

        double leftOnMainDock = Math.Clamp(buttonCenterOnMainDock - (categoryDockWidth / 2), 0, mainDockWidth - categoryDockWidth);
        return mainDockLeft + leftOnMainDock;
    }
}
