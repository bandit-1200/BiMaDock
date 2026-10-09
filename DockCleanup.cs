using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BiMaDock;

internal enum CleanupReason
{
    FileMissing,
    ShortcutTargetMissing,
    Unreachable,
    EmptyCategory
}

internal sealed record CleanupCandidate(DockItem Item, string CategoryName, CleanupReason Reason, string Description, bool PreSelected);

/// <summary>
/// Findet verwaiste Dock-Einträge (fehlende Dateien, kaputte Verknüpfungen, leere Kategorien)
/// und verwaltet eine einfache Sicherung der Dock-Einstellungen vor dem Aufräumen.
/// </summary>
internal static class DockCleanup
{
    public const string BackupFileName = "docksettings.backup.json";

    internal const string MainDockName = "Hauptdock";
    internal const string FileMissingDescription = "Datei oder Ordner nicht gefunden";
    internal const string ShortcutTargetMissingPrefix = "Verknüpfungsziel nicht gefunden: ";
    internal const string UnreachableDescription = "Laufwerk oder Netzwerkpfad derzeit nicht erreichbar";
    internal const string EmptyCategoryDescription = "Kategorie ist leer";
    internal const string CategoryEmptyAfterCleanupDescription = "Kategorie wäre nach dem Aufräumen leer";

    private static readonly TimeSpan ItemCheckTimeout = TimeSpan.FromSeconds(2);

    private readonly record struct CheckResult(CleanupReason Reason, string Description, bool PreSelected);

    /// <summary>
    /// Prüft alle Einträge (Hauptdock und Kategorien). Wirft keine Ausnahmen außer
    /// <see cref="OperationCanceledException"/> bei Abbruch über <paramref name="cancellationToken"/>.
    /// Dateiprüfungen laufen im Thread-Pool, jede Einzelprüfung ist auf etwa 2 Sekunden begrenzt.
    /// </summary>
    public static async Task<IReadOnlyList<CleanupCandidate>> FindCandidatesAsync(IReadOnlyList<DockItem> items, CancellationToken cancellationToken = default)
    {
        if (items is null || items.Count == 0)
        {
            return Array.Empty<CleanupCandidate>();
        }

        try
        {
            var categories = items
                .Where(item => item is not null && item.IsCategory)
                .OrderBy(item => item.Position)
                .ToList();
            var categoriesById = new Dictionary<string, DockItem>(StringComparer.Ordinal);
            foreach (var category in categories)
            {
                if (!string.IsNullOrEmpty(category.Id) && !categoriesById.ContainsKey(category.Id))
                {
                    categoriesById.Add(category.Id, category);
                }
            }

            var regularItems = items.Where(item => item is not null && !item.IsCategory).ToList();
            var mainItems = regularItems
                .Where(item => !IsInCategory(item, categoriesById))
                .OrderBy(item => item.Position)
                .ToList();

            // Einträge parallel prüfen; jede Prüfung ist einzeln zeitlich begrenzt.
            var checkTasks = regularItems
                .Select(item => CheckItemWithTimeoutAsync(item, cancellationToken))
                .ToArray();
            var checkResults = await Task.WhenAll(checkTasks).ConfigureAwait(false);
            var resultsByItem = new Dictionary<DockItem, CheckResult?>(ReferenceEqualityComparer.Instance);
            for (int i = 0; i < regularItems.Count; i++)
            {
                resultsByItem[regularItems[i]] = checkResults[i];
            }

            var candidates = new List<CleanupCandidate>();
            foreach (var item in mainItems)
            {
                AddCandidate(candidates, item, MainDockName, resultsByItem[item]);
            }

            var categoryCandidates = new List<CleanupCandidate>();
            foreach (var category in categories)
            {
                var children = categoriesById.TryGetValue(category.Id, out var registered) && ReferenceEquals(registered, category)
                    ? regularItems
                        .Where(item => string.Equals(item.Category, category.Id, StringComparison.Ordinal))
                        .OrderBy(item => item.Position)
                        .ToList()
                    : new List<DockItem>();

                string categoryName = string.IsNullOrWhiteSpace(category.DisplayName) ? category.Id : category.DisplayName;
                int childCandidateCount = 0;
                foreach (var child in children)
                {
                    if (AddCandidate(candidates, child, categoryName, resultsByItem[child]))
                    {
                        childCandidateCount++;
                    }
                }

                if (children.Count == 0)
                {
                    categoryCandidates.Add(new CleanupCandidate(category, MainDockName, CleanupReason.EmptyCategory, EmptyCategoryDescription, false));
                }
                else if (childCandidateCount == children.Count)
                {
                    categoryCandidates.Add(new CleanupCandidate(category, MainDockName, CleanupReason.EmptyCategory, CategoryEmptyAfterCleanupDescription, false));
                }
            }

            candidates.AddRange(categoryCandidates);
            return candidates;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            Trace.TraceError($"Dock cleanup check failed: {exception}");
            return Array.Empty<CleanupCandidate>();
        }
    }

    /// <summary>
    /// Liefert eine neue Liste ohne die angegebenen Einträge (Vergleich über die Id).
    /// Wird eine Kategorie entfernt, werden auch ihre Einträge entfernt.
    /// </summary>
    public static List<DockItem> RemoveItems(IReadOnlyList<DockItem> allItems, IEnumerable<DockItem> itemsToRemove)
    {
        var removeIds = new HashSet<string>(StringComparer.Ordinal);
        var removedCategoryIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in itemsToRemove ?? Enumerable.Empty<DockItem>())
        {
            if (item?.Id is null)
            {
                continue;
            }

            removeIds.Add(item.Id);
            if (item.IsCategory)
            {
                removedCategoryIds.Add(item.Id);
            }
        }

        // Auch Kategorien berücksichtigen, die nur über ihre Id übergeben wurden.
        foreach (var item in allItems)
        {
            if (item is not null && item.IsCategory && removeIds.Contains(item.Id))
            {
                removedCategoryIds.Add(item.Id);
            }
        }

        return allItems
            .Where(item => item is not null &&
                           !removeIds.Contains(item.Id) &&
                           (item.IsCategory || string.IsNullOrEmpty(item.Category) || !removedCategoryIds.Contains(item.Category)))
            .ToList();
    }

    public static bool HasBackup()
    {
        try
        {
            return File.Exists(GetBackupFilePath());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Schreibt die Sicherung atomar (temporäre Datei + Verschieben) und überschreibt eine vorhandene Sicherung.
    /// </summary>
    public static void CreateBackup(IReadOnlyList<DockItem> items)
    {
        AppPaths.EnsureAppDataDirectory();
        string backupPath = GetBackupFilePath();
        string json = JsonConvert.SerializeObject(items ?? Array.Empty<DockItem>(), Formatting.Indented);
        string temporaryPath = $"{backupPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, backupPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    /// <summary>
    /// Lädt die Sicherung; <c>null</c>, wenn keine vorhanden oder sie ungültig ist.
    /// </summary>
    public static List<DockItem>? LoadBackup()
    {
        try
        {
            string backupPath = GetBackupFilePath();
            if (!File.Exists(backupPath))
            {
                return null;
            }

            var items = JsonConvert.DeserializeObject<List<DockItem>>(File.ReadAllText(backupPath));
            return items?.Where(item => item is not null).ToList();
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            Trace.TraceError($"Dock cleanup backup could not be loaded: {exception}");
            return null;
        }
    }

    public static void DeleteBackup()
    {
        try
        {
            string backupPath = GetBackupFilePath();
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Trace.TraceError($"Dock cleanup backup could not be deleted: {exception}");
        }
    }

    private static string GetBackupFilePath() => Path.Combine(AppPaths.AppDataDirectory, BackupFileName);

    private static bool IsInCategory(DockItem item, Dictionary<string, DockItem> categoriesById) =>
        !string.IsNullOrEmpty(item.Category) && categoriesById.ContainsKey(item.Category);

    private static bool AddCandidate(List<CleanupCandidate> candidates, DockItem item, string categoryName, CheckResult? result)
    {
        if (result is not { } value)
        {
            return false;
        }

        candidates.Add(new CleanupCandidate(item, categoryName, value.Reason, value.Description, value.PreSelected));
        return true;
    }

    private static async Task<CheckResult?> CheckItemWithTimeoutAsync(DockItem item, CancellationToken cancellationToken)
    {
        string filePath = item.FilePath;
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return new CheckResult(CleanupReason.FileMissing, FileMissingDescription, true);
        }

        if (IsNonFileUri(filePath.Trim()))
        {
            return null;
        }

        try
        {
            return await Task.Run(() => CheckPath(filePath), cancellationToken)
                .WaitAsync(ItemCheckTimeout, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return new CheckResult(CleanupReason.Unreachable, UnreachableDescription, false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Unerwartete Fehler bei einer Einzelprüfung: Eintrag lieber behalten.
            Trace.TraceWarning($"Dock cleanup check for '{filePath}' failed: {exception}");
            return null;
        }
    }

    private static bool IsNonFileUri(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && !uri.IsFile;

    private static CheckResult? CheckPath(string rawPath)
    {
        string path = ExpandPath(rawPath);
        if (!Path.IsPathFullyQualified(path))
        {
            // Relative Angaben (z. B. "notepad.exe") werden über PATH/App Paths aufgelöst und sind nicht zuverlässig prüfbar.
            return null;
        }

        if (!IsLocationReachable(path))
        {
            return new CheckResult(CleanupReason.Unreachable, UnreachableDescription, false);
        }

        if (string.Equals(Path.GetExtension(path), ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(path))
            {
                return new CheckResult(CleanupReason.FileMissing, FileMissingDescription, true);
            }

            string? target = TryGetShortcutTarget(path);
            if (string.IsNullOrWhiteSpace(target) || !Path.IsPathFullyQualified(target) || IsNonFileUri(target))
            {
                // Shell-/UWP-Verknüpfungen ohne Dateipfad gelten als gültig.
                return null;
            }

            if (!IsLocationReachable(target))
            {
                return new CheckResult(CleanupReason.Unreachable, UnreachableDescription, false);
            }

            return File.Exists(target) || Directory.Exists(target)
                ? null
                : new CheckResult(CleanupReason.ShortcutTargetMissing, ShortcutTargetMissingPrefix + target, true);
        }

        return File.Exists(path) || Directory.Exists(path)
            ? null
            : new CheckResult(CleanupReason.FileMissing, FileMissingDescription, true);
    }

    private static string ExpandPath(string path) =>
        Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));

    /// <summary>
    /// Prüft vor dem eigentlichen Dateizugriff, ob Netzwerkpfad bzw. Wechsel-/Netzlaufwerk erreichbar ist.
    /// </summary>
    private static bool IsLocationReachable(string path)
    {
        try
        {
            string? root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root))
            {
                return true;
            }

            if (root.StartsWith(@"\\", StringComparison.Ordinal))
            {
                // UNC-Pfad (\\server\freigabe\ bzw. \\?\UNC\...): Freigabe muss erreichbar sein.
                if (root.StartsWith(@"\\?\", StringComparison.Ordinal) || root.StartsWith(@"\\.\", StringComparison.Ordinal))
                {
                    bool isUnc = root.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase);
                    if (!isUnc)
                    {
                        string remainder = path.Substring(4);
                        return remainder.Length < 2 || remainder[1] != ':' || IsDriveReachable(remainder.Substring(0, 3));
                    }
                }

                return Directory.Exists(root);
            }

            return IsDriveReachable(root);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    private static bool IsDriveReachable(string root)
    {
        var drive = new DriveInfo(root);
        switch (drive.DriveType)
        {
            case DriveType.Network:
            case DriveType.Removable:
            case DriveType.CDRom:
                return drive.IsReady && Directory.Exists(drive.RootDirectory.FullName);
            case DriveType.NoRootDirectory:
                // Laufwerksbuchstabe existiert derzeit nicht (z. B. abgezogener USB-Stick).
                return false;
            default:
                return true;
        }
    }

    private static string? TryGetShortcutTarget(string shortcutPath)
    {
        object? link = null;
        try
        {
            Type? shellLinkType = Type.GetTypeFromCLSID(ShellLinkClsid, throwOnError: false);
            if (shellLinkType is null)
            {
                return null;
            }

            // ShellLink unterstützt beide Threading-Modelle und funktioniert daher auch im MTA-Thread-Pool.
            link = Activator.CreateInstance(shellLinkType);
            if (link is not IShellLinkW shellLink || link is not IPersistFile persistFile)
            {
                return null;
            }

            persistFile.Load(shortcutPath, StgmRead);
            var buffer = new StringBuilder(MaxPathLength);
            shellLink.GetPath(buffer, buffer.Capacity, IntPtr.Zero, SlgpRawPath);
            string target = buffer.ToString().Trim();
            return string.IsNullOrEmpty(target) ? null : Environment.ExpandEnvironmentVariables(target);
        }
        catch (Exception exception) when (exception is COMException or InvalidCastException or UnauthorizedAccessException or IOException or ArgumentException)
        {
            Trace.TraceWarning($"Shortcut '{shortcutPath}' could not be resolved: {exception.Message}");
            return null;
        }
        finally
        {
            if (link is not null && Marshal.IsComObject(link))
            {
                Marshal.FinalReleaseComObject(link);
            }
        }
    }

    private static readonly Guid ShellLinkClsid = new("00021401-0000-0000-C000-000000000046");
    private const int StgmRead = 0;
    private const uint SlgpRawPath = 0x4;
    private const int MaxPathLength = 32768;

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}
