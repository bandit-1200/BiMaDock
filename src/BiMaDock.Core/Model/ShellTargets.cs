using System.Text.RegularExpressions;

namespace BiMaDock.Core.Model;

/// <summary>
/// Ziele, die keine Datei sind, sondern ein Eintrag der Windows-Programmliste ("shell:AppsFolder\..."),
/// z. B. Store-Apps oder Programme aus dem Startmenü ohne Verknüpfungsdatei.
/// </summary>
public static partial class ShellTargets
{
    public const string AppsFolderPrefix = @"shell:AppsFolder\";

    public static bool IsShellTarget(string? target) =>
        target is not null && target.TrimStart().StartsWith("shell:", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Wählt das Ziel für ein Shell-Element aus dem Startmenü: echter Pfad, wenn es einen gibt (Verknüpfung, Programm),
    /// sonst "shell:AppsFolder\&lt;Name&gt;". Null, wenn nichts Brauchbares da ist.
    /// </summary>
    /// <param name="fileSystemPath">Dateipfad laut Shell (SIGDN_FILESYSPATH) oder null</param>
    /// <param name="parsingName">Name der Programmliste, z. B. AUMID oder "{GUID}\Ordner\app.exe"</param>
    /// <param name="knownFolder">Löst eine Known-Folder-GUID in einen Ordnerpfad auf</param>
    /// <param name="exists">Prüft, ob eine Datei existiert</param>
    public static string? Classify(string? fileSystemPath, string? parsingName, Func<Guid, string?> knownFolder, Func<string, bool> exists)
    {
        if (!string.IsNullOrWhiteSpace(fileSystemPath) && Path.IsPathFullyQualified(fileSystemPath))
        {
            return fileSystemPath;
        }

        if (string.IsNullOrWhiteSpace(parsingName))
        {
            return null;
        }

        string name = parsingName.Trim();
        if (IsShellTarget(name))
        {
            return name;
        }

        if (Path.IsPathFullyQualified(name))
        {
            return name;
        }

        if (name.StartsWith("::", StringComparison.Ordinal))
        {
            return null;
        }

        // "{GUID}\Unterordner\app.exe": Win32-Programm, dessen Ordner über eine Known-Folder-GUID angegeben ist
        var match = KnownFolderPath().Match(name);
        if (match.Success && Guid.TryParse(match.Groups[1].Value, out var guid) && knownFolder(guid) is { Length: > 0 } folder)
        {
            string path = Path.Combine(folder, match.Groups[2].Value);
            if (exists(path))
            {
                return path;
            }
        }

        return AppsFolderPrefix + name;
    }

    /// <summary>Anzeigename aus einem Ziel, falls die Shell keinen liefert.</summary>
    public static string FallbackName(string target)
    {
        string name = target.StartsWith(AppsFolderPrefix, StringComparison.OrdinalIgnoreCase) ? target[AppsFolderPrefix.Length..] : target;
        int bang = name.IndexOf('!');
        if (bang > 0)
        {
            name = name[..bang];
        }

        name = name[(name.LastIndexOf('\\') + 1)..];
        int underscore = name.IndexOf('_');
        if (underscore > 0)
        {
            name = name[..underscore];
        }

        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && name.Length > 4)
        {
            name = name[..^4];
        }

        return string.IsNullOrWhiteSpace(name) ? target : name;
    }

    [GeneratedRegex(@"^\{([0-9A-Fa-f\-]{36})\}\\(.+)$")]
    private static partial Regex KnownFolderPath();
}

/// <summary>Aufbau des Zwischenablageformats "Shell IDList Array" (CIDA): Anzahl, Offsets, dann die Item-IDs.</summary>
public static class ShellIdListArray
{
    /// <summary>Liest die Offsets: Ordner-IDList und je Element eine IDList. Null, wenn die Daten nicht passen.</summary>
    public static (int Parent, int[] Children)? ParseOffsets(ReadOnlySpan<byte> data)
    {
        if (data.Length < 8)
        {
            return null;
        }

        uint count = BitConverter.ToUInt32(data[..4]);
        if (count == 0 || count > 10000)
        {
            return null;
        }

        long headerSize = 4 + ((count + 1) * 4L);
        if (data.Length < headerSize)
        {
            return null;
        }

        var offsets = new int[count + 1];
        for (int i = 0; i < offsets.Length; i++)
        {
            uint offset = BitConverter.ToUInt32(data.Slice(4 + (i * 4), 4));
            if (offset < headerSize || offset >= data.Length)
            {
                return null;
            }

            offsets[i] = (int)offset;
        }

        return (offsets[0], offsets[1..]);
    }
}
