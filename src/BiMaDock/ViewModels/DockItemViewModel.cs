using System.Windows.Media;
using BiMaDock.Core.Model;
using BiMaDock.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BiMaDock.ViewModels;

public sealed partial class DockItemViewModel : ObservableObject
{
    private readonly IconService _icons;
    private ImageSource? _icon;

    public DockItemViewModel(DockEntry entry, IconService icons)
    {
        Entry = entry;
        _icons = icons;
    }

    public DockEntry Entry { get; }

    public string Name => Entry.Name;

    public bool IsCategory => Entry.IsCategory;

    /// <summary>Ziel liegt im Dateisystem und kann im Explorer gezeigt werden.</summary>
    public bool CanReveal => !IsCategory && !IconService.IsWebLink(Entry.Target);

    /// <summary>Wird erst geladen, wenn die Oberfläche das Symbol anzeigt.</summary>
    public ImageSource Icon => _icon ??= _icons.GetIcon(Entry);

    /// <summary>Symbol neu laden, z. B. nachdem ein Bibliothekssymbol entfernt wurde.</summary>
    public void InvalidateIcon()
    {
        _icon = null;
        OnPropertyChanged(nameof(Icon));
    }

    /// <summary>Kategorie ist gerade im Kategorie-Dock geöffnet.</summary>
    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    /// <summary>Beim Ziehen: Ablegen landet in dieser Kategorie.</summary>
    [ObservableProperty]
    public partial bool IsDropTarget { get; set; }

    /// <summary>Gerade hinzugefügt: beim ersten Anzeigen ploppt das Symbol herein.</summary>
    public bool IsNew { get; set; }
}
