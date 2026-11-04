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
        foreach (var entry in _layout.MainEntries)
        {
            MainItems.Add(new DockItemViewModel(entry, _icons));
        }

        _logger.LogInformation("{Count} Einträge geladen, davon {Main} im Hauptdock", _layout.Entries.Count, MainItems.Count);
    }

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

    partial void OnOpenCategoryChanged(DockItemViewModel? oldValue, DockItemViewModel? newValue)
    {
        oldValue?.IsOpen = false;
        CategoryItems.Clear();
        if (newValue is null)
        {
            return;
        }

        newValue.IsOpen = true;
        foreach (var entry in _layout.EntriesOf(newValue.Entry.Id))
        {
            CategoryItems.Add(new DockItemViewModel(entry, _icons));
        }
    }
}
