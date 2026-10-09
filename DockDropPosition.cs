using System.Windows;
using System.Windows.Controls;

namespace BiMaDock;

internal static class DockDropPosition
{
    internal const string DockItemButtonFormat = "BiMaDock.DockItemButton";
    private static readonly string[] FileNameFormats = { "FileNameW", "FileName" };

    internal static bool HasFilePathData(IDataObject data)
    {
        return data.GetDataPresent(DataFormats.FileDrop) ||
               FileNameFormats.Any(data.GetDataPresent);
    }

    internal static string[]? GetDroppedFilePaths(IDataObject data)
    {
        if (data.GetDataPresent(DataFormats.FileDrop) &&
            data.GetData(DataFormats.FileDrop) is string[] fileDropPaths)
        {
            return fileDropPaths;
        }

        foreach (string format in FileNameFormats)
        {
            if (!data.GetDataPresent(format))
            {
                continue;
            }

            object? value = data.GetData(format);
            if (value is string path)
            {
                return new[] { path };
            }

            if (value is string[] paths)
            {
                return paths;
            }
        }

        return null;
    }

    internal static int FindInsertionIndex(IReadOnlyList<double> itemCenters, double dropX)
    {
        for (int i = 0; i < itemCenters.Count; i++)
        {
            if (dropX < itemCenters[i])
            {
                return i;
            }
        }

        return itemCenters.Count;
    }

    internal static int FindInsertionIndex(Panel panel, double dropX)
    {
        var buttons = panel.Children.OfType<Button>().ToList();
        var itemCenters = buttons
            .Select(button => button.TranslatePoint(new Point(0, 0), panel).X + button.RenderSize.Width / 2)
            .ToList();
        int buttonIndex = FindInsertionIndex(itemCenters, dropX);

        return buttonIndex < buttons.Count
            ? panel.Children.IndexOf(buttons[buttonIndex])
            : panel.Children.Count;
    }
}
