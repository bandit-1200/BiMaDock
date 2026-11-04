using System.Windows;
using System.Windows.Media;
using BiMaDock.Core.Cleanup;
using BiMaDock.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BiMaDock.Views;

/// <summary>Eine Zeile im Aufräumen-Dialog.</summary>
public sealed partial class CleanupRow(CleanupCandidate candidate, ImageSource icon) : ObservableObject
{
    public CleanupCandidate Candidate { get; } = candidate;

    public ImageSource Icon { get; } = icon;

    public string Name => Candidate.Entry.Name;

    public string Detail => Candidate.Reason switch
    {
        CleanupReason.Missing => $"Nicht mehr vorhanden: {Candidate.Entry.Target}",
        CleanupReason.Unreachable => $"Gerade nicht erreichbar (Netzwerk/USB?): {Candidate.Entry.Target}",
        _ => "Leere Kategorie",
    };

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = candidate.SelectedByDefault;
}

public partial class CleanupDialog : DialogWindow
{
    private readonly List<CleanupRow> _rows;

    private CleanupDialog(IReadOnlyList<CleanupCandidate> candidates, IconService icons)
    {
        InitializeComponent();
        _rows = candidates.Select(c => new CleanupRow(c, icons.GetIcon(c.Entry))).ToList();
        CandidateList.ItemsSource = _rows;
        Summary.Text = $"{_rows.Count} Einträge gefunden. Fehlende Ziele sind vorausgewählt, unsichere Fälle nicht.";
        foreach (var row in _rows)
        {
            row.PropertyChanged += (_, _) => UpdateButton();
        }

        UpdateButton();
    }

    /// <summary>Gibt die IDs der zu entfernenden Einträge zurück, oder null bei Abbruch.</summary>
    public static IReadOnlyCollection<Guid>? Choose(Window? owner, IReadOnlyList<CleanupCandidate> candidates, IconService icons)
    {
        var dialog = new CleanupDialog(candidates, icons);
        return dialog.ShowFor(owner) == true
            ? dialog._rows.Where(r => r.IsSelected).Select(r => r.Candidate.Entry.Id).ToList()
            : null;
    }

    private void UpdateButton()
    {
        int count = _rows.Count(r => r.IsSelected);
        RemoveButton.IsEnabled = count > 0;
        RemoveButton.Content = count > 0 ? $"{count} entfernen" : "Entfernen";
    }

    private void Remove_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
