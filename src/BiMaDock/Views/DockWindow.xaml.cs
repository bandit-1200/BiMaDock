using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BiMaDock.Native;
using BiMaDock.ViewModels;
using Microsoft.Win32;

namespace BiMaDock.Views;

/// <summary>
/// Fenster am oberen Bildschirmrand. Kümmert sich nur um Darstellung: Ein-/Ausblenden,
/// Position und Animation des Kategorie-Docks. Zustand und Aktionen liegen im <see cref="DockViewModel"/>.
/// </summary>
public partial class DockWindow : Window
{
    /// <summary>So viel vom Hauptdock bleibt im ausgeblendeten Zustand sichtbar (Auslöser zum Einblenden).</summary>
    private const double PeekHeight = 4;

    private static readonly Duration SlideDuration = TimeSpan.FromMilliseconds(170);
    private static readonly Duration FlowDuration = TimeSpan.FromMilliseconds(220);

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
            HideDock();
        };

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        viewModel.CategoryItems.CollectionChanged += (_, _) =>
            EmptyCategoryHint.Visibility = viewModel.CategoryItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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
            if (!DockArea.IsMouseOver && !_isMenuOpen)
            {
                HideDock();
            }
        };
        Loaded += (_, _) => HideDock(animate: false);
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
            SlideTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, null);
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
            SlideTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, null);
            SlideTransform.Y = _isShown ? 0 : HiddenOffset;
        };
        SlideTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, animation);
    }

    // ---------- Kategorie-Dock ----------

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
            CategoryDock.Visibility = Visibility.Collapsed;
        }
    }

    private void OpenCategoryDock(DockItemViewModel category)
    {
        CategoryOffset.X = 0;
        CategoryDock.Visibility = Visibility.Visible;
        DockArea.UpdateLayout();

        if (MainItemsControl.ItemContainerGenerator.ContainerFromItem(category) is not FrameworkElement icon)
        {
            return;
        }

        // Kategorie-Dock mittig unter das Symbol legen, ohne über den Bildschirmrand hinauszuragen.
        double iconCenter = icon.TranslatePoint(new Point(icon.ActualWidth / 2, 0), Root).X;
        double width = CategoryDock.ActualWidth;
        double naturalLeft = CategoryDock.TranslatePoint(new Point(0, 0), Root).X;
        double desiredLeft = Math.Clamp(iconCenter - (width / 2), 8, Math.Max(8, Root.ActualWidth - width - 8));
        CategoryOffset.X = desiredLeft - naturalLeft;

        // Aus dem Symbol herausfließen: Skalierung mit Ursprung über dem Kategorie-Symbol.
        CategoryDock.RenderTransformOrigin = new Point(Math.Clamp((iconCenter - desiredLeft) / width, 0, 1), 0);
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var grow = new DoubleAnimation(0.2, 1, FlowDuration) { EasingFunction = easing };
        CategoryScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, grow);
        CategoryScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, grow);
        CategoryDock.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, FlowDuration) { EasingFunction = easing });
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
