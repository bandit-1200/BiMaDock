using System.Collections.ObjectModel;
using BiMaDock.Core.Model;
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
    private readonly DockStore _store;
    private readonly IconService _icons;
    private readonly ILauncher _launcher;
    private readonly ILogger<DockViewModel> _logger;
    private DockLayout _layout = new();

    public DockViewModel(DockStore store, IconService icons, ILauncher launcher, ILogger<DockViewModel> logger)
    {
        _store = store;
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

    public string DataFile => _store.FilePath;

    public void Load()
    {
        _layout = _store.Load();
        _icons.ClearCache();
        OpenCategory = null;
        MainItems.Clear();
        CategoryItems.Clear();
        Sync();
        _logger.LogInformation("{Count} Einträge geladen, davon {Main} im Hauptdock", _layout.Entries.Count, MainItems.Count);
    }

    // ---------- Aktionen ----------

    [RelayCommand]
    private void Activate(DockItemViewModel? item)
    {
        if (item is null)
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

    [RelayCommand]
    private void CloseCategory() => OpenCategory = null;

    public DockItemViewModel? FindItem(Guid id) =>
        MainItems.FirstOrDefault(i => i.Entry.Id == id) ?? CategoryItems.FirstOrDefault(i => i.Entry.Id == id);

    /// <summary>Fügt abgelegte Dateien, Ordner oder Weblinks ein. <paramref name="dockId"/> null = Hauptdock.</summary>
    public int AddFromDrop(IEnumerable<string> values, Guid? dockId, int index)
    {
        int added = 0;
        foreach (var value in values)
        {
            if (DockEntryFactory.FromDropValue(value) is not { } entry)
            {
                _logger.LogInformation("Abgelegter Wert ignoriert: {Value}", value);
                continue;
            }

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
        if (_layout.Move(id, dockId, index))
        {
            Commit();
        }
    }

    public void AddCategory(string name)
    {
        _layout.Insert(DockEntryFactory.NewCategory(name), null, int.MaxValue);
        Commit();
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
            .Select(e => existing.TryGetValue(e.Id, out var vm) && vm.Entry == e ? vm : new DockItemViewModel(e, _icons))
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
            CategoryItems.Add(new DockItemViewModel(entry, _icons));
        }
    }
}
