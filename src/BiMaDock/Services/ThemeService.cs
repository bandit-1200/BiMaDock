using System.Windows;
using System.Windows.Media;
using BiMaDock.Core.Settings;

namespace BiMaDock.Services;

/// <summary>
/// Überträgt die Farbeinstellungen auf die Pinsel der Oberfläche. Die Einträge in den Anwendungsressourcen
/// überdecken die Standardwerte aus <c>Theme.xaml</c>; alle Stellen verwenden DynamicResource und ändern sich sofort.
/// </summary>
public static class ThemeService
{
    public static void Apply(AppSettings settings, ResourceDictionary resources)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(resources);

        var dock = Parse(settings.DockColor);
        byte alpha = (byte)Math.Round(settings.DockOpacity * 255 / 100.0);
        var border = Parse(settings.BorderColor);
        var accent = Parse(settings.AccentColor);

        resources["DockSurfaceBrush"] = Frozen(Color.FromArgb(alpha, dock.R, dock.G, dock.B));
        resources["LiquidBrush"] = Frozen(Color.FromArgb(alpha, dock.R, dock.G, dock.B));
        resources["SurfaceBorderBrush"] = Frozen(Color.FromArgb(0x38, border.R, border.G, border.B));
        resources["AccentBrush"] = Frozen(accent);
        resources["AccentSoftBrush"] = Frozen(Color.FromArgb(0x40, accent.R, accent.G, accent.B));

        var feedback = Parse(settings.FeedbackColor);
        resources["DropFeedbackColor"] = Color.FromArgb(alpha, feedback.R, feedback.G, feedback.B);
        resources["LabelVisibility"] = settings.ShowLabels ? Visibility.Visible : Visibility.Collapsed;
        BiMaDock.Views.ItemAnimations.Configure(settings);
    }

    public static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    private static SolidColorBrush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
