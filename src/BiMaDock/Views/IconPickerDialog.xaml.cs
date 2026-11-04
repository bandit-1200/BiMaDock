using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BiMaDock.Core.Icons;
using BiMaDock.Services;
using Microsoft.Win32;

namespace BiMaDock.Views;

/// <summary>
/// Symbolbibliothek: durchsuchen, nach Kategorie filtern, eigene Symbole hinzufügen und entfernen.
/// Im Auswahlmodus liefert der Dialog die Referenz des gewählten Symbols.
/// </summary>
public partial class IconPickerDialog : DialogWindow
{
    private const string AllCategories = "Alle";

    private readonly IconService _icons;
    private readonly bool _pickMode;
    private IReadOnlyList<LibraryIcon> _all = [];
    private string _category = AllCategories;
    private LibraryIcon? _selected;

    private IconPickerDialog(IconService icons, bool pickMode)
    {
        _icons = icons;
        _pickMode = pickMode;
        InitializeComponent();

        TitleText.Text = pickMode ? "Symbol wählen" : "Symbolbibliothek";
        if (!pickMode)
        {
            ChooseButton.Visibility = Visibility.Collapsed;
            CancelButton.Content = "Schließen";
        }

        AllowDrop = true;
        Drop += Dialog_Drop;
        Loaded += (_, _) => SearchBox.Focus();
        Reload();
    }

    /// <summary>Öffnet die Auswahl und gibt die Referenz des Symbols zurück, oder null bei Abbruch.</summary>
    public static string? Pick(Window? owner, IconService icons)
    {
        var dialog = new IconPickerDialog(icons, pickMode: true);
        return dialog.ShowFor(owner) == true ? dialog._selected?.Reference : null;
    }

    /// <summary>Öffnet die Bibliothek zum Verwalten.</summary>
    public static void Manage(Window? owner, IconService icons) => new IconPickerDialog(icons, pickMode: false).ShowFor(owner);

    private void Reload(LibraryIcon? select = null)
    {
        _all = _icons.Library.Load();
        BuildCategories();
        _selected = select is null ? null : _all.FirstOrDefault(i => i.Reference == select.Reference);
        BuildIcons();
    }

    private void BuildCategories()
    {
        var categories = _all.Select(i => i.Category).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
        if (_category != AllCategories && !categories.Contains(_category))
        {
            _category = AllCategories;
        }

        CategoryPanel.Children.Clear();
        foreach (string category in categories.Prepend(AllCategories))
        {
            var chip = new Border
            {
                Tag = category,
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1.5),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 6, 6),
                Cursor = Cursors.Hand,
                Child = new TextBlock { Text = category, Foreground = Brushes.White },
            };
            chip.MouseLeftButtonDown += (_, e) =>
            {
                e.Handled = true;
                _category = category;
                MarkCategories();
                BuildIcons();
            };
            CategoryPanel.Children.Add(chip);
        }

        MarkCategories();
    }

    private void MarkCategories()
    {
        var accent = (Brush)FindResource("AccentBrush");
        foreach (Border chip in CategoryPanel.Children)
        {
            bool selected = (string)chip.Tag == _category;
            chip.BorderBrush = selected ? accent : new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));
            chip.Background = selected ? new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)) : Brushes.Transparent;
        }
    }

    private void BuildIcons()
    {
        string search = SearchBox.Text.Trim();
        var visible = _all.Where(i =>
            (_category == AllCategories || i.Category == _category)
            && (search.Length == 0 || i.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                || i.Category.Contains(search, StringComparison.CurrentCultureIgnoreCase)));

        IconPanel.Children.Clear();
        foreach (var icon in visible)
        {
            IconPanel.Children.Add(CreateTile(icon));
        }

        if (IconPanel.Children.Count == 0)
        {
            IconPanel.Children.Add(new TextBlock { Text = "Keine Symbole gefunden.", Foreground = Brushes.Gray, Margin = new Thickness(10) });
        }

        UpdateSelection();
    }

    private Border CreateTile(LibraryIcon icon)
    {
        var tile = new Border
        {
            Tag = icon,
            Width = 84,
            Height = 88,
            Margin = new Thickness(3),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1.5),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            ToolTip = $"{icon.Name} ({icon.Category}{(icon.IsBuiltIn ? string.Empty : ", eigenes Symbol")})",
            Child = new StackPanel
            {
                Children =
                {
                    new Image
                    {
                        Source = _icons.GetLibraryIcon(icon),
                        Width = 48,
                        Height = 48,
                        Margin = new Thickness(0, 8, 0, 4),
                    },
                    new TextBlock
                    {
                        Text = icon.Name,
                        Foreground = Brushes.White,
                        FontSize = 11,
                        TextAlignment = TextAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        Margin = new Thickness(4, 0, 4, 0),
                    },
                },
            },
        };
        RenderOptions.SetBitmapScalingMode(tile, BitmapScalingMode.HighQuality);
        tile.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            _selected = icon;
            UpdateSelection();
            if (e.ClickCount == 2 && _pickMode)
            {
                DialogResult = true;
            }
        };
        return tile;
    }

    private void UpdateSelection()
    {
        var accent = (Brush)FindResource("AccentBrush");
        foreach (var tile in IconPanel.Children.OfType<Border>())
        {
            bool selected = ReferenceEquals(tile.Tag, _selected);
            tile.BorderBrush = selected ? accent : Brushes.Transparent;
            tile.Background = selected ? new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)) : Brushes.Transparent;
        }

        ChooseButton.IsEnabled = _selected is not null;
        DeleteButton.IsEnabled = _selected is { IsBuiltIn: false };
    }

    private void Filter_Changed(object sender, TextChangedEventArgs e)
    {
        if (IsInitialized)
        {
            BuildIcons();
        }
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Eigene Symbole hinzufügen",
            Filter = "Bilder|*.png;*.ico;*.jpg;*.jpeg",
            Multiselect = true,
        };
        if (dialog.ShowDialog(this) == true)
        {
            Import(dialog.FileNames);
        }
    }

    private void Dialog_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            Import(files);
        }
    }

    private void Import(IEnumerable<string> files)
    {
        LibraryIcon? last = null;
        var failed = new List<string>();
        foreach (string file in files)
        {
            try
            {
                last = _icons.Library.Import(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                failed.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
        }

        if (last is not null)
        {
            _category = IconLibrary.UserCategory;
            Reload(last);
        }

        StatusText.Text = failed.Count == 0 ? "Übernommen." : "Nicht übernommen: " + string.Join("; ", failed);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is not { IsBuiltIn: false } icon)
        {
            return;
        }

        try
        {
            _icons.Library.Delete(icon);
            _icons.ClearCache();
            StatusText.Text = $"„{icon.Name}“ entfernt. Einträge, die es nutzen, zeigen wieder ihr Standardsymbol.";
            Reload();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = "Entfernen fehlgeschlagen: " + ex.Message;
        }
    }

    private void Choose_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
