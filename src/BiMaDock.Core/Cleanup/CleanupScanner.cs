using BiMaDock.Core.Model;

namespace BiMaDock.Core.Cleanup;

public enum PathState
{
    Exists,
    Missing,

    /// <summary>Netzlaufwerk, USB-Stick oder Freigabe gerade nicht erreichbar: vielleicht nur vorübergehend.</summary>
    Unreachable,
}

public enum CleanupReason
{
    Missing,
    Unreachable,
    EmptyCategory,
}

/// <summary>Vorschlag zum Entfernen. Fehlende lokale Ziele sind vorausgewählt, alles Unsichere nicht.</summary>
public sealed record CleanupCandidate(DockEntry Entry, CleanupReason Reason)
{
    public bool SelectedByDefault => Reason == CleanupReason.Missing;
}

/// <summary>
/// Findet Einträge, deren Ziel nicht mehr existiert, und leere Kategorien. Weblinks werden nicht geprüft.
/// </summary>
public static class CleanupScanner
{
    public static List<CleanupCandidate> Scan(DockLayout layout, Func<string, PathState>? probe = null)
    {
        ArgumentNullException.ThrowIfNull(layout);
        probe ??= ProbePath;
        var result = new List<CleanupCandidate>();
        var removedIds = new HashSet<Guid>();

        foreach (var entry in layout.Entries.Where(e => !e.IsCategory))
        {
            if (IsWebLink(entry.Target))
            {
                continue;
            }

            var state = string.IsNullOrWhiteSpace(entry.Target) ? PathState.Missing : probe(entry.Target);
            if (state == PathState.Exists)
            {
                continue;
            }

            var reason = state == PathState.Missing ? CleanupReason.Missing : CleanupReason.Unreachable;
            result.Add(new CleanupCandidate(entry, reason));
            if (reason == CleanupReason.Missing)
            {
                removedIds.Add(entry.Id);
            }
        }

        // Kategorien, die leer sind oder nach dem Entfernen der vorausgewählten Einträge leer wären
        foreach (var category in layout.Entries.Where(e => e.IsCategory))
        {
            if (layout.EntriesOf(category.Id).All(e => removedIds.Contains(e.Id)))
            {
                result.Add(new CleanupCandidate(category, CleanupReason.EmptyCategory));
            }
        }

        return result;
    }

    public static PathState ProbePath(string path)
    {
        try
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                return PathState.Exists;
            }

            if (path.StartsWith(@"\\", StringComparison.Ordinal))
            {
                return PathState.Unreachable;
            }

            string? root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root))
            {
                return PathState.Missing;
            }

            // Laufwerk fehlt (USB abgezogen) oder ist ein Netzlaufwerk: nicht sicher, ob das Ziel wirklich weg ist.
            var drive = new DriveInfo(root);
            return !drive.IsReady || drive.DriveType == DriveType.Network ? PathState.Unreachable : PathState.Missing;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return PathState.Unreachable;
        }
    }

    private static bool IsWebLink(string target) =>
        Uri.TryCreate(target, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
