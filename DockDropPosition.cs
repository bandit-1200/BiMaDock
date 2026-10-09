using System.IO;
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

    internal static bool IsSupportedDrop(IDataObject data)
    {
        return data.GetDataPresent(DockItemButtonFormat) ||
               HasFilePathData(data) ||
               TryGetLinkText(data, out _);
    }

    // Liest einen Link aus Browser-Drags (UniformResourceLocator) oder aus Text.
    // Akzeptiert werden nur absolute http-, https- oder file-URIs.
    internal static bool TryGetLinkText(IDataObject data, out string link)
    {
        link = string.Empty;
        string[] formats =
        {
            "UniformResourceLocatorW",
            "UniformResourceLocator",
            DataFormats.UnicodeText,
            DataFormats.Text
        };

        foreach (string format in formats)
        {
            string? raw;
            try
            {
                if (!data.GetDataPresent(format))
                {
                    continue;
                }

                raw = ReadText(data.GetData(format), format == "UniformResourceLocatorW");
            }
            catch (Exception)
            {
                continue;
            }

            if (TryNormalizeLink(raw, out link))
            {
                return true;
            }
        }

        link = string.Empty;
        return false;
    }

    private static string? ReadText(object? value, bool unicode)
    {
        switch (value)
        {
            case string text:
                return text;
            case string[] lines:
                return lines.FirstOrDefault();
            case MemoryStream stream:
                byte[] bytes = stream.ToArray();
                string decoded = unicode
                    ? System.Text.Encoding.Unicode.GetString(bytes)
                    : System.Text.Encoding.Default.GetString(bytes);
                return decoded.Split('\0')[0];
            default:
                return null;
        }
    }

    private static bool TryNormalizeLink(string? raw, out string link)
    {
        link = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        string firstLine = raw.Trim().Split('\r', '\n')[0].Trim();
        if (firstLine.Length == 0 ||
            !Uri.TryCreate(firstLine, UriKind.Absolute, out Uri? uri) ||
            !(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeFile))
        {
            return false;
        }

        link = firstLine;
        return true;
    }

    // Liefert einen lesbaren Anzeigenamen für Dateien, Ordner, Laufwerke und URLs; nie leer.
    internal static string GetDisplayName(string pathOrUrl)
    {
        if (string.IsNullOrWhiteSpace(pathOrUrl))
        {
            return pathOrUrl ?? string.Empty;
        }

        string trimmed = pathOrUrl.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            string host = uri.Host;
            if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                host = host.Substring(4);
            }

            return host.Length > 0 ? host : pathOrUrl;
        }

        try
        {
            string path = trimmed;
            if (uri != null && uri.IsFile)
            {
                path = uri.LocalPath;
            }

            string withoutTrailing = path.TrimEnd('\\', '/');
            if (withoutTrailing.Length == 0)
            {
                return pathOrUrl;
            }

            // Laufwerk, z. B. "C:\" oder "C:"
            if (withoutTrailing.Length == 2 && withoutTrailing[1] == ':')
            {
                return withoutTrailing.ToUpperInvariant();
            }

            bool isDirectory = path.EndsWith('\\') || path.EndsWith('/') || Directory.Exists(path);
            string name = isDirectory
                ? Path.GetFileName(withoutTrailing)
                : Path.GetFileNameWithoutExtension(withoutTrailing);

            if (string.IsNullOrWhiteSpace(name))
            {
                name = Path.GetFileName(withoutTrailing);
            }

            return string.IsNullOrWhiteSpace(name) ? pathOrUrl : name;
        }
        catch (ArgumentException)
        {
            return pathOrUrl;
        }
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
