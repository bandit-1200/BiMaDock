using System.Windows;
using BiMaDock.Core.Model;
using BiMaDock.Services;
using Microsoft.Win32;

namespace BiMaDock.Views;

public partial class EditEntryDialog : DialogWindow
{
    private readonly DockEntry _original;
    private readonly IconService _icons;

    private EditEntryDialog(DockEntry entry, IconService icons)
    {
        _original = entry;
        _icons = icons;
        InitializeComponent();

        TitleText.Text = entry.IsCategory ? "Kategorie bearbeiten" : "Eintrag bearbeiten";
        NameBox.Text = entry.Name;
        TargetBox.Text = entry.Target;
        IconBox.Text = entry.IconPath ?? string.Empty;
        TargetPanel.Visibility = entry.IsCategory ? Visibility.Collapsed : Visibility.Visible;
        NameBox.TextChanged += Field_Changed;
        TargetBox.TextChanged += Field_Changed;
        Loaded += (_, _) =>
        {
            NameBox.Focus();
            NameBox.SelectAll();
        };
        UpdatePreview();
    }

    private DockEntry Result => _original with
    {
        Name = NameBox.Text.Trim(),
        Target = TargetBox.Text.Trim(),
        IconPath = string.IsNullOrWhiteSpace(IconBox.Text) ? null : IconBox.Text.Trim(),
    };

    /// <summary>Gibt den geänderten Eintrag zurück, oder null bei Abbruch.</summary>
    public static DockEntry? Edit(Window? owner, DockEntry entry, IconService icons)
    {
        var dialog = new EditEntryDialog(entry, icons);
        return dialog.ShowFor(owner) == true ? dialog.Result : null;
    }

    private void Field_Changed(object sender, RoutedEventArgs e) => UpdatePreview();

    private void UpdatePreview()
    {
        if (SaveButton is null)
        {
            return;
        }

        Preview.Source = _icons.GetIcon(Result);
        SaveButton.IsEnabled = NameBox.Text.Trim().Length > 0 && (_original.IsCategory || TargetBox.Text.Trim().Length > 0);
    }

    private void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Ziel wählen", DereferenceLinks = false, Filter = "Alle Dateien|*.*" };
        if (dialog.ShowDialog(this) == true)
        {
            TargetBox.Text = dialog.FileName;
        }
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Ordner wählen" };
        if (dialog.ShowDialog(this) == true)
        {
            TargetBox.Text = dialog.FolderName;
        }
    }

    private void BrowseIcon_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Symbol wählen",
            Filter = "Bilder und Symbole|*.png;*.jpg;*.ico;*.exe;*.dll|Alle Dateien|*.*",
        };
        if (dialog.ShowDialog(this) == true)
        {
            IconBox.Text = dialog.FileName;
        }
    }

    private void LibraryIcon_Click(object sender, RoutedEventArgs e)
    {
        if (IconPickerDialog.Pick(this, _icons) is { } reference)
        {
            IconBox.Text = reference;
        }
    }

    private void ResetIcon_Click(object sender, RoutedEventArgs e) => IconBox.Text = string.Empty;

    private void Save_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
