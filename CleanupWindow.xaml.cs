using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace BiMaDock
{
    public partial class CleanupWindow : Window
    {
        private readonly List<CandidateViewModel> candidates;

        internal CleanupWindow(IReadOnlyList<CleanupCandidate> candidates)
        {
            ArgumentNullException.ThrowIfNull(candidates);

            InitializeComponent();

            this.candidates = candidates.Select(candidate => new CandidateViewModel(candidate)).ToList();
            foreach (var candidate in this.candidates)
            {
                candidate.PropertyChanged += Candidate_PropertyChanged;
            }

            CandidateList.ItemsSource = this.candidates;
            SummaryText.Text = BuildSummary(this.candidates.Count);
            UpdateRemoveButtonState();
        }

        internal IReadOnlyList<DockItem> SelectedItems { get; private set; } = Array.Empty<DockItem>();

        private static string BuildSummary(int count)
        {
            return count == 1
                ? "1 Eintrag verweist auf ein nicht mehr vorhandenes Ziel. Ausgewählte Einträge werden aus dem Dock entfernt."
                : $"{count} Einträge verweisen auf nicht mehr vorhandene Ziele. Ausgewählte Einträge werden aus dem Dock entfernt.";
        }

        private void Candidate_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CandidateViewModel.Selected))
            {
                UpdateRemoveButtonState();
            }
        }

        private void UpdateRemoveButtonState()
        {
            bool anySelected = candidates.Any(candidate => candidate.Selected);
            RemoveSelectedButton.IsEnabled = anySelected;
            RemoveSelectedButton.Opacity = anySelected ? 1.0 : 0.5;
        }

        private void SetAllSelected(bool selected)
        {
            foreach (var candidate in candidates)
            {
                candidate.Selected = selected;
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            SetAllSelected(true);
        }

        private void SelectNoneButton_Click(object sender, RoutedEventArgs e)
        {
            SetAllSelected(false);
        }

        private void RemoveSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedItems = candidates
                .Where(candidate => candidate.Selected)
                .Select(candidate => candidate.Item)
                .ToList();

            if (SelectedItems.Count == 0)
            {
                return;
            }

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedItems = Array.Empty<DockItem>();
            DialogResult = false;
        }

        internal sealed class CandidateViewModel : INotifyPropertyChanged
        {
            private bool selected;
            private ImageSource? icon;

            public CandidateViewModel(CleanupCandidate candidate)
            {
                Item = candidate.Item;
                selected = candidate.PreSelected;
                DisplayName = string.IsNullOrWhiteSpace(candidate.Item.DisplayName)
                    ? candidate.Item.FilePath
                    : candidate.Item.DisplayName;
                CategoryLabel = string.IsNullOrWhiteSpace(candidate.CategoryName)
                    ? "Ohne Kategorie"
                    : candidate.CategoryName;
                Path = candidate.Item.FilePath;
                Description = candidate.Description;
                IsHint = candidate.Reason == CleanupReason.Unreachable;
                AutomationId = "CleanupItem_" + candidate.Item.Id;
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            public DockItem Item { get; }

            public string DisplayName { get; }

            public string CategoryLabel { get; }

            public string Path { get; }

            public string Description { get; }

            public bool IsHint { get; }

            public string AutomationId { get; }

            public ImageSource Icon
            {
                get
                {
                    if (icon == null)
                    {
                        string iconSource = string.IsNullOrWhiteSpace(Item.IconSource) ? Item.FilePath : Item.IconSource;
                        icon = IconHelper.GetIcon(Item.FilePath, iconSource);
                    }

                    return icon;
                }
            }

            public bool Selected
            {
                get => selected;
                set
                {
                    if (selected == value)
                    {
                        return;
                    }

                    selected = value;
                    OnPropertyChanged();
                }
            }

            private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
