using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BiMaDock.Native;
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

    private static readonly Duration SlideDuration = TimeSpan.FromMilliseconds(170);

    private readonly DockViewModel _viewModel;
    private readonly DispatcherTimer _showTimer;
    private readonly DispatcherTimer _hideTimer;
    private bool _isShown = true;
    private bool _isMenuOpen;

    public DockWindow(DockViewModel viewModel)
    {
        _viewModel = viewModel;
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
        if (!animate)
        {
            SlideTransform.BeginAnimation(TranslateTransform.YProperty, null);
            SlideTransform.Y = to;
            return;
        }

        var animation = new DoubleAnimation(to, SlideDuration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        animation.Completed += (_, _) =>
        {
            // Endwert festschreiben, damit spätere Größenänderungen den Wert setzen können.
            SlideTransform.BeginAnimation(TranslateTransform.YProperty, null);
            SlideTransform.Y = _isShown ? 0 : HiddenOffset;
        };
        SlideTransform.BeginAnimation(TranslateTransform.YProperty, animation);
    }

    // ---------- Kontextmenü ----------

    private void ContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        _isMenuOpen = true;
        _hideTimer.Stop();
    }

    private void ContextMenu_Closed(object sender, RoutedEventArgs e)
    {
        _isMenuOpen = false;
        if (!DockArea.IsMouseOver)
        {
            _hideTimer.Start();
        }
    }

    private void NewCategory_Click(object sender, RoutedEventArgs e)
    {
        _isMenuOpen = true;
        try
        {
            string? name = InputDialog.Ask(this, "Neue Kategorie", "Name der Kategorie:");
            if (name is not null)
            {
                _viewModel.AddCategory(name);
            }
        }
        finally
        {
            _isMenuOpen = false;
        }
    }

    private void Reload_Click(object sender, RoutedEventArgs e) => _viewModel.Load();

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
