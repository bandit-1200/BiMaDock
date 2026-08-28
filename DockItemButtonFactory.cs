using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

/// <summary>
/// Erstellt die visuelle Darstellung eines Dock-Elements.
/// Ereignisse und Anwendungslogik verbleiben beim DockManager.
/// </summary>
internal static class DockItemButtonFactory
{
    public static Button Create(DockItem item)
    {
        string iconSource = !string.IsNullOrEmpty(item.IconSource) ? item.IconSource : item.FilePath;
        var image = new Image
        {
            Source = IconHelper.GetIcon(item.FilePath, iconSource),
            Width = 32,
            Height = 32,
            Margin = new Thickness(5)
        };
        var textBlock = new TextBlock
        {
            Text = item.DisplayName,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Width = 60,
            Margin = new Thickness(5)
        };
        var content = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 70
        };
        content.Children.Add(image);
        content.Children.Add(textBlock);

        return new Button
        {
            Content = content,
            Tag = item,
            Margin = new Thickness(5),
            Width = 70,
            ToolTip = new ToolTip
            {
                Content = new TextBlock
                {
                    Text = item.DisplayName,
                    FontFamily = new FontFamily("Arial"),
                    FontSize = 16,
                    Foreground = Brushes.DarkBlue
                },
                Placement = PlacementMode.Center,
                HorizontalOffset = 0,
                VerticalOffset = 55
            }
        };
    }
}
