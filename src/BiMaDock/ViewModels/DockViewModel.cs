using System.Collections.ObjectModel;
using BiMaDock.Core.Cleanup;
using BiMaDock.Core.Model;
using BiMaDock.Core.Settings;
using BiMaDock.Core.Storage;
using BiMaDock.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace BiMaDock.ViewModels;

/// <summary>
/// Zustand und Aktionen von Haupt- und Kategorie-Dock. Kennt keine Fenster oder Steuerelemente.
/// </summary>
public sealed partial class DockViewModel : ObservableObject
{
    private const int MaxUndoSteps = 20;

    private readonly DockStore _store;
    private readonly SettingsStore _settingsStore;
    private readonly Stack<(string Description, DockLayout Layout)> _undo = new();
    private readonly IconService _icons;
    private readonly ILauncher _launcher;
    private readonly ILogger<DockViewModel> _logger;
    private DockLayout _layout = new();
    private DateTime _lastTargetCheck = DateTime.MinValue;
    private bool _targetCheckRunning;

    public DockViewModel(DockStore store, SettingsStore settingsStore, IconService icons, ILauncher launcher, ILogger<DockViewModel> logger)
    {
        _store = store;
        _settingsStore = settingsStore;
        Settings = settingsStore.Load();
        _icons = icons;
        _launcher = launcher;
        _logger = logger;
    }

    /// <summary>Meldung, die der Nutzer sehen soll (z. B. Ziel nicht gefunden).</summary>
    public event EventHandler<string>? ErrorOccurred;

    /// <summary>Ein Eintrag wurde gestartet; das Dock darf sich ausblenden.</summary>
    public event EventHandler? Launched;

    public ObservableCollection<DockItemViewModel> MainItems { get; } = [];

    /// <summary>Einträge der geöffneten Kategorie. Bleibt beim Schließen stehen, damit die Schließ-Animation sie noch zeigt.</summary>
    public ObservableCollection<DockItemViewModel> CategoryItems { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCategoryOpen))]
    public partial DockItemViewModel? OpenCategory { get; set; }

    public bool IsCategoryOpen => OpenCategory is not null;

    [ObservableProperty]
    public partial AppSettings Settings { get; private set; }

    /// <summary>Beschreibung des letzten rückgängig machbaren Schritts, null wenn keiner.</summary>
    [ObservableProperty]
    public partial string? UndoDescription { get; private set; }

    public string DataFile => _store.FilePath;

    /// <summary>Alle angezeigten Symbole neu laden.</summary>
    public void RefreshIcons()
    {
        _icons.ClearCache();
        foreach (var item in MainItems.Concat(CategoryItems))
        {
            item.InvalidateIcon();
        }
    }

    /// <summary>Einträge in der Reihenfolge, in der ihre Symbole vorgeladen werden sollen.</summary>
    public IReadOnlyList<DockEntry> IconPreloadOrder() => BiMaDock.Core.Icons.IconPreloadOrder.Sort(_layout);

    public void Load()
    {
        _layout = _store.Load();
        AdoptLegacyIcons();
        _icons.ClearCache();
        OpenCategory = null;
        MainItems.Clear();
        CategoryItems.Clear();
        Sync();
        _logger.LogInformation("{Count} Einträge geladen, davon {Main} im Hauptdock", _layout.Entries.Count, MainItems.Count);
        _ = CheckTargetsAsync(force: true);
    }

    /// <summary>
    /// Symbole, die noch im Ordner des alten Docks liegen, in die eigenen Symbole kopieren. Sonst wären sie weg,
    /// sobald das alte Dock deinstalliert wird.
    /// </summary>
    private void AdoptLegacyIcons()
    {
        try
        {
            int count = BiMaDock.Core.Icons.LegacyIconAdopter.Adopt(_layout, _icons.Library);
            if (count > 0)
            {
                _store.Save(_layout);
                _logger.LogInformation("{Count} Symbole aus dem alten Dock in die eigenen Symbole übernommen", count);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _logger.LogError(ex, "Symbole des alten Docks konnten nicht übernommen werden");
        }
    }

    /// <summary>
    /// Prüft im Hintergrund, welche Ziele fehlen, und markiert sie. Höchstens alle 10 Sekunden (wird beim
    /// Einblenden aufgerufen). Nicht erreichbare Netzlaufwerke gelten nicht als fehlend.
    /// </summary>
    public async Task CheckTargetsAsync(bool force = false)
    {
        if (_targetCheckRunning || (!force && DateTime.UtcNow - _lastTargetCheck < TimeSpan.FromSeconds(10)))
        {
            return;
        }

        _targetCheckRunning = true;
        _lastTargetCheck = DateTime.UtcNow;
        try
        {
            var targets = _layout.Entries
                .Where(e => e.Kind == DockEntryKind.Launch && !IconService.IsWebLink(e.Target))
                .Select(e => e.Target)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var missing = await Task.Run(() => targets
                .Where(t => string.IsNullOrWhiteSpace(t) || CleanupScanner.ProbePath(t) == PathState.Missing)
                .ToHashSet(StringComparer.OrdinalIgnoreCase));

            // Auch Einträge in nicht geöffneten Kategorien: deren ViewModels entstehen erst beim Öffnen
            if (missing.Count > 0 && !missing.SetEquals(_missingTargets))
            {
                _logger.LogInformation("{Count} Ziele nicht gefunden: {Targets}", missing.Count, string.Join(", ", missing));
            }

            _missingTargets = missing;
            foreach (var item in MainItems.Concat(CategoryItems))
            {
                ApplyMissing(item);
            }
        }
        finally
        {
            _targetCheckRunning = false;
        }
    }

    private HashSet<string> _missingTargets = new(StringComparer.OrdinalIgnoreCase);

    private void ApplyMissing(DockItemViewModel item) =>
        item.IsMissing = item.Entry.Kind == DockEntryKind.Launch && _missingTargets.Contains(item.Entry.Target);

    // ---------- Aktionen ----------

    [RelayCommand]
    private void Activate(DockItemViewModel? item)
    {
        if (item is null || item.IsSeparator)
        {
            return;
        }

        if (item.IsCategory)
        {
            OpenCategory = OpenCategory == item ? null : item;
            return;
        }

        string? error = _launcher.Launch(item.Entry);
        if (error is not null)
        {
            ErrorOccurred?.Invoke(this, error);
            return;
        }

        OpenCategory = null;
        Launched?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Startet den Eintrag einmalig mit Adminrechten, unabhängig von seiner Einstellung.</summary>
    public void LaunchAsAdmin(DockItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!item.AcceptsFiles)
        {
            return;
        }

        if (_launcher.Launch(item.Entry with { RunAsAdmin = true }) is { } error)
        {
            ErrorOccurred?.Invoke(this, error);
            return;
        }

        OpenCategory = null;
        Launched?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Öffnet abgelegte Dateien mit dem Programm des Eintrags.</summary>
    public void OpenFilesWith(DockItemViewModel item, IReadOnlyList<string> files)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (files.Count == 0 || !item.AcceptsFiles)
        {
            return;
        }

        if (_launcher.Launch(item.Entry, files) is { } error)
        {
            ErrorOccurred?.Invoke(this, error);
            return;
        }

        OpenCategory = null;
        Launched?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void CloseCategory() => OpenCategory = null;

    public DockItemViewModel? FindItem(Guid id) =>
        MainItems.FirstOrDefault(i => i.Entry.Id == id) ?? CategoryItems.FirstOrDefault(i => i.Entry.Id == id);

    /// <summary>Fügt abgelegte Dateien, Ordner oder Weblinks ein. <paramref name="dockId"/> null = Hauptdock.</summary>
    /// <param name="names">Anzeigenamen für Werte, die Windows selbst benennt (Startmenü), sonst null.</param>
    public int AddFromDrop(IEnumerable<string> values, Guid? dockId, int index, IReadOnlyDictionary<string, string>? names = null)
    {
        int added = 0;
        foreach (var value in values)
        {
            string? displayName = null;
            names?.TryGetValue(value, out displayName);
            if (DockEntryFactory.FromShellItem(value, displayName) is not { } entry)
            {
                _logger.LogInformation("Abgelegter Wert ignoriert: {Value}", value);
                continue;
            }

            _logger.LogInformation("Abgelegt: {Name} -> {Target}", entry.Name, entry.Target);
            _layout.Insert(entry, dockId, index == int.MaxValue ? index : index + added);
            added++;
        }

        if (added > 0)
        {
            Commit();
        }

        return added;
    }

    public void MoveEntry(Guid id, Guid? dockId, int index)
    {
        var before = _layout.Clone();
        if (_layout.Move(id, dockId, index))
        {
            PushUndo("Verschieben", before);
            Commit();
        }
    }

    public void AddCategory(string name)
    {
        _layout.Insert(DockEntryFactory.NewCategory(name), null, int.MaxValue);
        Commit();
    }

    /// <summary>Fügt rechts neben <paramref name="item"/> einen Trenner ein, im selben Dock.</summary>
    public void InsertSeparatorAfter(DockItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var before = _layout.Clone();
        Guid? dockId = item.Entry.CategoryId;
        int index = _layout.EntriesIn(dockId).FindIndex(e => e.Id == item.Entry.Id);
        _layout.Insert(DockEntryFactory.NewSeparator(), dockId, index < 0 ? int.MaxValue : index + 1);
        PushUndo("Trenner einfügen", before);
        Commit();
    }

    public void RemoveEntry(DockItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var before = _layout.Clone();
        if (_layout.Remove(item.Entry.Id) > 0)
        {
            PushUndo($"„{item.Name}“ entfernen", before);
            Commit();
        }
    }

    public void UpdateEntry(DockEntry updated)
    {
        var before = _layout.Clone();
        if (_layout.Update(updated))
        {
            PushUndo($"„{updated.Name}“ bearbeiten", before);
            _icons.ClearCache();
            Commit();
        }
    }

    /// <summary>Anzahl der Einträge, die beim Löschen einer Kategorie mit entfernt würden.</summary>
    public int CountEntriesIn(DockItemViewModel category) => _layout.EntriesOf(category.Entry.Id).Count();

    public List<CleanupCandidate> FindCleanupCandidates() => CleanupScanner.Scan(_layout);

    public void RemoveEntries(IReadOnlyCollection<Guid> ids)
    {
        var before = _layout.Clone();
        int removed = ids.Sum(_layout.Remove);
        if (removed > 0)
        {
            PushUndo($"Aufräumen ({removed} Einträge)", before);
            Commit();
            _logger.LogInformation("Aufräumen: {Count} Einträge entfernt", removed);
        }
    }

    [RelayCommand]
    private void Undo()
    {
        if (_undo.Count == 0)
        {
            return;
        }

        var (description, layout) = _undo.Pop();
        _layout = layout;
        _icons.ClearCache();
        UndoDescription = _undo.Count > 0 ? _undo.Peek().Description : null;
        _logger.LogInformation("Rückgängig: {Description}", description);
        Commit();
    }

    public void SaveSettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Settings = settings.Normalized();
        try
        {
            _settingsStore.Save(Settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Einstellungen konnten nicht gespeichert werden");
            ErrorOccurred?.Invoke(this, $"Die Einstellungen konnten nicht gespeichert werden:\n{ex.Message}");
        }
    }

    private void PushUndo(string description, DockLayout before)
    {
        _undo.Push((description, before));
        if (_undo.Count > MaxUndoSteps)
        {
            var keep = _undo.Take(MaxUndoSteps).Reverse().ToList();
            _undo.Clear();
            keep.ForEach(_undo.Push);
        }

        UndoDescription = description;
    }

    // ---------- Speichern und Abgleich ----------

    private void Commit()
    {
        try
        {
            _store.Save(_layout);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Speichern fehlgeschlagen");
            ErrorOccurred?.Invoke(this, $"Die Änderung konnte nicht gespeichert werden:\n{ex.Message}");
        }

        Sync();
    }

    /// <summary>
    /// Gleicht die sichtbaren Listen mit dem Layout ab. Unveränderte Einträge behalten ihr ViewModel,
    /// damit die Oberfläche nur verschiebt statt neu aufzubauen.
    /// </summary>
    private void Sync()
    {
        var existing = MainItems.Concat(CategoryItems).GroupBy(i => i.Entry.Id).ToDictionary(g => g.Key, g => g.First());

        SyncCollection(MainItems, _layout.MainEntries, existing);

        if (OpenCategory is { } open)
        {
            var current = MainItems.FirstOrDefault(i => i.Entry.Id == open.Entry.Id);
            if (current is null)
            {
                OpenCategory = null;
            }
            else if (!ReferenceEquals(current, open))
            {
                // Kategorie selbst wurde geändert (z. B. umbenannt): neu öffnen
                OpenCategory = current;
            }
            else
            {
                SyncCollection(CategoryItems, _layout.EntriesOf(open.Entry.Id), existing);
            }
        }
    }

    private void SyncCollection(ObservableCollection<DockItemViewModel> collection, IEnumerable<DockEntry> entries, Dictionary<Guid, DockItemViewModel> existing)
    {
        var target = entries
            .Select(e => existing.TryGetValue(e.Id, out var vm) && vm.Entry == e
                ? vm
                : CreateItem(e, isNew: existing.Count > 0 && !existing.ContainsKey(e.Id)))
            .ToList();

        for (int i = 0; i < target.Count; i++)
        {
            if (i < collection.Count && ReferenceEquals(collection[i], target[i]))
            {
                continue;
            }

            int found = -1;
            for (int j = i + 1; j < collection.Count; j++)
            {
                if (ReferenceEquals(collection[j], target[i]))
                {
                    found = j;
                    break;
                }
            }

            if (found >= 0)
            {
                collection.Move(found, i);
            }
            else
            {
                collection.Insert(i, target[i]);
            }
        }

        while (collection.Count > target.Count)
        {
            collection.RemoveAt(collection.Count - 1);
        }
    }

    private DockItemViewModel CreateItem(DockEntry entry, bool isNew)
    {
        var item = new DockItemViewModel(entry, _icons) { IsNew = isNew };
        ApplyMissing(item);
        return item;
    }

    partial void OnOpenCategoryChanged(DockItemViewModel? oldValue, DockItemViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.IsOpen = false;
        }

        if (newValue is null)
        {
            return;
        }

        newValue.IsOpen = true;
        CategoryItems.Clear();
        foreach (var entry in _layout.EntriesOf(newValue.Entry.Id))
        {
            CategoryItems.Add(CreateItem(entry, isNew: false));
        }
    }
}
