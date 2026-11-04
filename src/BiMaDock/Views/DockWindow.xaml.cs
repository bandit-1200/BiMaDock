using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BiMaDock.Core.Settings;
using BiMaDock.Native;
using BiMaDock.Services;
using BiMaDock.ViewModels;
using Microsoft.Win32;

namespace BiMaDock.Views;

/// <summary>
/// Fenster am oberen Bildschirmrand. Kümmert sich nur um Darstellung: Ein-/Ausblenden (diese Datei),
/// Kategorie-Animation (<c>DockWindow.CategoryFlow.cs</c>) und Drag &amp; Drop (<c>DockWindow.DragDrop.cs</c>).
/// Zustand und Aktionen liegen im <see cref="DockViewModel"/>.
/// </summary>
public partial class DockWindow : Window
{
    /// <summary>So viel vom Hauptdock bleibt im ausgeblendeten Zustand sichtbar (Auslöser zum Einblenden).</summary>
    private const double PeekHeight = 4;

    private readonly DockViewModel _viewModel;
    private readonly IconService _icons;
    private readonly StartupService _startup;
    private readonly DispatcherTimer _showTimer;
    private readonly DispatcherTimer _hideTimer;
    private bool _isShown = true;
    private bool _isMenuOpen;

    public DockWindow(DockViewModel viewModel, IconService icons, StartupService startup)
    {
        _viewModel = viewModel;
        _icons = icons;
        _startup = startup;
        DataContext = viewModel;
        InitializeComponent();

        _showTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _showTimer.Tick += (_, _) =>
        {
            _showTimer.Stop();
            ShowDock();
        };
        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            if (!IsDragActive)
            {
                HideDock();
            }
        };

        ApplySettings();
        HookMagnify(MainItemsControl);
        HookMagnify(CategoryItemsControl);
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        viewModel.MainItems.CollectionChanged += (_, _) => UpdateHints();
        viewModel.CategoryItems.CollectionChanged += (_, _) => UpdateHints();
        viewModel.Launched += (_, _) => HideDock();
        viewModel.ErrorOccurred += (_, message) =>
            MessageBox.Show(this, message, "BiMaDock", MessageBoxButton.OK, MessageBoxImage.Warning);

        MainDock.SizeChanged += (_, _) =>
        {
            if (!_isShown)
            {
                SlideTransform.Y = HiddenOffset;
            }
        };
        Deactivated += (_, _) =>
        {
            if (!DockArea.IsMouseOver && !_isMenuOpen && !IsDragActive)
            {
                HideDock();
            }
        };
        Loaded += (_, _) =>
        {
            UpdateHints();
            HideDock(animate: false);
        };
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        Closed += (_, _) => SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

        PlaceOnScreen();
    }

    private double HiddenOffset => -(MainDock.ActualHeight - PeekHeight);

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowStyles.MakeToolWindow(new WindowInteropHelper(this).Handle);
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e) => Dispatcher.BeginInvoke(PlaceOnScreen);

    private void PlaceOnScreen()
    {
        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
    }

    private void UpdateHints()
    {
        EmptyMainHint.Visibility = _viewModel.MainItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyCategoryHint.Visibility = _viewModel.CategoryItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DockViewModel.Settings))
        {
            ApplySettings();
            return;
        }

        if (e.PropertyName != nameof(DockViewModel.OpenCategory))
        {
            return;
        }

        if (_viewModel.OpenCategory is { } category)
        {
            OpenCategoryDock(category);
        }
        else
        {
            CloseCategoryDock();
        }
    }

    // ---------- Ein- und Ausblenden ----------

    private void DockArea_MouseEnter(object sender, MouseEventArgs e)
    {
        _hideTimer.Stop();
        if (!_isShown)
        {
            _showTimer.Start();
        }
    }

    private void DockArea_MouseLeave(object sender, MouseEventArgs e)
    {
        _showTimer.Stop();
        if (_isShown && !_isMenuOpen)
        {
            _hideTimer.Start();
        }
    }

    private void ShowDock()
    {
        _showTimer.Stop();
        _hideTimer.Stop();
        if (_isShown)
        {
            return;
        }

        _isShown = true;
        Slide(0, animate: true);
    }

    private void HideDock(bool animate = true)
    {
        _showTimer.Stop();
        _hideTimer.Stop();
        _viewModel.CloseCategoryCommand.Execute(null);
        _isShown = false;
        Slide(HiddenOffset, animate);
    }

    private void Slide(double to, bool animate)
    {
        if (!animate || _viewModel.Settings.SlideStyle == SlideStyle.None)
        {
            SlideTransform.BeginAnimation(TranslateTransform.YProperty, null);
            SlideTransform.Y = to;
            return;
        }

        var settings = _viewModel.Settings;
        bool showing = to == 0;
        var animation = new DoubleAnimation(to, TimeSpan.FromMilliseconds(settings.SlideDurationMs * (showing ? 1.0 : 0.75)))
        {
            // Beim Einblenden federt das Dock je nach Einstellung kurz nach; Ausblenden ist immer glatt.
            EasingFunction = showing && settings.SlideStyle == SlideStyle.Spring
                ? new BackEase { Amplitude = 0.35, EasingMode = EasingMode.EaseOut }
                : new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        animation.Completed += (_, _) =>
        {
            // Endwert festschreiben, damit spätere Größenänderungen den Wert setzen können.
            SlideTransform.BeginAnimation(TranslateTransform.YProperty, null);
            SlideTransform.Y = _isShown ? 0 : HiddenOffset;
        };
        SlideTransform.BeginAnimation(TranslateTransform.YProperty, animation);
    }

    // ---------- Kontextmenüs ----------

    private void DockArea_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        _isMenuOpen = true;
        _hideTimer.Stop();
    }

    private void DockArea_ContextMenuClosing(object sender, ContextMenuEventArgs e)
    {
        _isMenuOpen = false;
        if (!DockArea.IsMouseOver)
        {
            _hideTimer.Start();
        }
    }

    private void Menu_Opened(object sender, RoutedEventArgs e)
    {
        foreach (var item in ((ContextMenu)sender).Items.OfType<MenuItem>().Where(m => Equals(m.Tag, "Undo")))
        {
            item.IsEnabled = _viewModel.UndoDescription is not null;
            item.Header = _viewModel.UndoDescription is { } description ? $"Rückgängig: {description}" : "Rückgängig";
        }
    }

    private static DockItemViewModel? ItemOf(object sender) => (sender as FrameworkElement)?.DataContext as DockItemViewModel;

    /// <summary>Dialoge halten das Dock offen, solange sie angezeigt werden.</summary>
    private T WithDockKeptOpen<T>(Func<T> showDialog)
    {
        _isMenuOpen = true;
        _hideTimer.Stop();
        try
        {
            return showDialog();
        }
        finally
        {
            _isMenuOpen = false;
            if (!DockArea.IsMouseOver)
            {
                _hideTimer.Start();
            }
        }
    }

    private void ItemOpen_Click(object sender, RoutedEventArgs e) => _viewModel.ActivateCommand.Execute(ItemOf(sender));

    private void ItemReveal_Click(object sender, RoutedEventArgs e)
    {
        if (ItemOf(sender) is not { } item)
        {
            return;
        }

        string target = item.Entry.Target;
        if (!File.Exists(target) && !Directory.Exists(target))
        {
            MessageBox.Show(this, $"„{item.Name}“ wurde nicht gefunden:\n{target}", "BiMaDock", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var startInfo = new ProcessStartInfo("explorer.exe") { UseShellExecute = true };
        startInfo.ArgumentList.Add($"/select,{target}");
        using var _ = Process.Start(startInfo);
    }

    private void ItemEdit_Click(object sender, RoutedEventArgs e)
    {
        if (ItemOf(sender) is not { } item)
        {
            return;
        }

        var updated = WithDockKeptOpen(() => EditEntryDialog.Edit(this, item.Entry, _icons));
        if (updated is not null)
        {
            _viewModel.UpdateEntry(updated);
        }
    }

    private void ItemRemove_Click(object sender, RoutedEventArgs e)
    {
        if (ItemOf(sender) is not { } item)
        {
            return;
        }

        int content = item.IsCategory ? _viewModel.CountEntriesIn(item) : 0;
        if (content > 0)
        {
            var answer = WithDockKeptOpen(() => MessageBox.Show(this,
                $"Die Kategorie „{item.Name}“ enthält {content} Einträge. Kategorie samt Inhalt entfernen?\n\n(„Rückgängig“ im Kontextmenü stellt sie wieder her.)",
                "BiMaDock", MessageBoxButton.YesNo, MessageBoxImage.Question));
            if (answer != MessageBoxResult.Yes)
            {
                return;
            }
        }

        if (ButtonOf(item) is { } button)
        {
            button.IsHitTestVisible = false;
            ItemAnimations.ShrinkOut(button, () => _viewModel.RemoveEntry(item));
        }
        else
        {
            _viewModel.RemoveEntry(item);
        }
    }

    private void Undo_Click(object sender, RoutedEventArgs e) => _viewModel.UndoCommand.Execute(null);

    private void IconLibrary_Click(object sender, RoutedEventArgs e)
    {
        WithDockKeptOpen(() =>
        {
            IconPickerDialog.Manage(this, _icons);
            return true;
        });
        _viewModel.RefreshIcons();
    }

    private void Cleanup_Click(object sender, RoutedEventArgs e)
    {
        var candidates = _viewModel.FindCleanupCandidates();
        if (candidates.Count == 0)
        {
            WithDockKeptOpen(() => MessageBox.Show(this, "Alles in Ordnung: Alle Ziele sind vorhanden und keine Kategorie ist leer.",
                "BiMaDock", MessageBoxButton.OK, MessageBoxImage.Information));
            return;
        }

        var ids = WithDockKeptOpen(() => CleanupDialog.Choose(this, candidates, _icons));
        if (ids is { Count: > 0 })
        {
            _viewModel.RemoveEntries(ids);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var result = WithDockKeptOpen(() => SettingsDialog.Edit(this, _viewModel.Settings, _startup.IsEnabled, _icons, preview => ThemeService.Apply(preview, Application.Current.Resources)));
        if (result is not { } chosen)
        {
            return;
        }

        _viewModel.SaveSettings(chosen.Settings);
        if (chosen.Autostart != _startup.IsEnabled && !_startup.TrySetEnabled(chosen.Autostart))
        {
            MessageBox.Show(this, "Der Autostart konnte nicht geändert werden. Details im Protokoll.", "BiMaDock",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ApplySettings()
    {
        var settings = _viewModel.Settings;
        ThemeService.Apply(settings, Application.Current.Resources);
        _showTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(1, settings.ShowDelayMs));
        _hideTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(1, settings.HideDelayMs));
    }

    private void NewCategory_Click(object sender, RoutedEventArgs e)
    {
        string? name = WithDockKeptOpen(() => InputDialog.Ask(this, "Neue Kategorie", "Name der Kategorie:"));
        if (name is not null)
        {
            _viewModel.AddCategory(name);
        }
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        string? folder = Path.GetDirectoryName(_viewModel.DataFile);
        if (folder is not null && Directory.Exists(folder))
        {
            using var _ = Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
