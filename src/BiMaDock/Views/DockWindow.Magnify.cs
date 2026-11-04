using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BiMaDock.Views;

/// <summary>Lupe wie beim Mac: Symbole unter der Maus wachsen, Nachbarn wachsen etwas mit.</summary>
public partial class DockWindow
{
    /// <summary>Zustand der Lupe pro Leiste.</summary>
    private sealed class MagnifyState
    {
        public bool Inside;
        public double MouseX; // relativ zur Leiste (tatsächliche, vergrößerte Lage)

        /// <summary>Zwischengespeicherte Buttons; null = neu suchen (nach Änderungen an der Liste).</summary>
        public List<Button>? Buttons;
    }

    private readonly Dictionary<ItemsControl, MagnifyState> _magnify = [];
    private TimeSpan _lastMagnifyFrame;
    private bool _magnifyRunning;

    /// <summary>Mit Umgebungsvariable BIMADOCK_PERF=1 schreibt die Lupe Bildrate und Rechenzeit ins Protokoll.</summary>
    private static readonly bool PerfLogging = Environment.GetEnvironmentVariable("BIMADOCK_PERF") == "1";
    private System.Diagnostics.Stopwatch? _perfWatch;
    private System.Diagnostics.Stopwatch? _perfSw;
    private int _perfFrames;
    private Microsoft.Extensions.Logging.ILogger? _perfLog;
    private double _perfWorst;
    private double _perfWork;

    /// <summary>
    /// Die Lupe läuft im Takt der Bildwiederholung statt bei jedem Mausereignis: Jedes Bild nähert sich jeder
    /// Faktor weich seinem Ziel. Die Ziele werden aus den Ruhepositionen der Symbole berechnet, nicht aus den
    /// vergrößerten, sonst schaukeln sich Mausposition und Layout gegenseitig auf (Zittern).
    /// </summary>
    private void HookMagnify(ItemsControl items, FrameworkElement surface)
    {
        // Maus an der ganzen Dock-Fläche verfolgen: Zwischen zwei Symbolen ist keine Trefferfläche,
        // sonst fiele die Lupe dort kurz zusammen.
        var state = new MagnifyState();
        _magnify[items] = state;
        ((System.Collections.Specialized.INotifyCollectionChanged)items.Items).CollectionChanged += (_, _) => state.Buttons = null;
        items.ItemContainerGenerator.StatusChanged += (_, _) => state.Buttons = null;
        surface.MouseEnter += (_, e) => { state.Inside = true; TrackMouse(items, state, e); };
        surface.MouseMove += (_, e) => { state.Inside = true; TrackMouse(items, state, e); };
        surface.MouseLeave += (_, _) => { state.Inside = false; StartMagnifyLoop(); };
    }

    private void TrackMouse(ItemsControl items, MagnifyState state, MouseEventArgs e)
    {
        if (!ItemAnimations.Settings.MagnifyEnabled || IsDragActive || e.LeftButton == MouseButtonState.Pressed)
        {
            state.Inside = false;
        }
        else
        {
            state.MouseX = e.GetPosition(items).X;
        }

        StartMagnifyLoop();
    }

    private void StartMagnifyLoop()
    {
        if (!_magnifyRunning)
        {
            _magnifyRunning = true;
            _lastMagnifyFrame = TimeSpan.Zero;
            CompositionTarget.Rendering += MagnifyFrame;
        }
    }

    private void MagnifyFrame(object? sender, EventArgs e)
    {
        var now = ((RenderingEventArgs)e).RenderingTime;
        if (now == _lastMagnifyFrame)
        {
            return; // gleiches Bild doppelt gemeldet
        }

        double dt = _lastMagnifyFrame == TimeSpan.Zero ? 1.0 / 60 : Math.Min((now - _lastMagnifyFrame).TotalSeconds, 0.05);
        _lastMagnifyFrame = now;

        // Zeitkonstante aus der eingestellten Reaktionszeit, unabhängig von der Bildrate
        double tau = Math.Max(ItemAnimations.Settings.MagnifySpeedMs, 16) / 3000.0;
        double blend = 1 - Math.Exp(-dt / tau);

        if (PerfLogging)
        {
            _perfFrames++;
            _perfWorst = Math.Max(_perfWorst, dt);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _perfWatch ??= System.Diagnostics.Stopwatch.StartNew();
            if (_perfWatch.Elapsed.TotalSeconds >= 2)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(_perfLog ??= (Microsoft.Extensions.Logging.ILogger)((App)Application.Current).Services.GetService(typeof(Microsoft.Extensions.Logging.ILogger<DockWindow>))!, "Lupe: {Fps:0} Bilder/s, langsamstes {Worst:0} ms, Rechenzeit {Work:0.0} ms/Bild",
                    _perfFrames / _perfWatch.Elapsed.TotalSeconds, _perfWorst * 1000, _perfWork / Math.Max(1, _perfFrames));
                _perfFrames = 0;
                _perfWorst = 0;
                _perfWork = 0;
                _perfWatch.Restart();
            }

            _perfSw = sw;
        }

        bool busy = false;
        foreach (var (items, state) in _magnify)
        {
            var buttons = state.Buttons is { } cached && cached.Count == items.Items.Count ? cached : state.Buttons = ItemButtons(items);
            busy |= ItemAnimations.StepMagnify(buttons, items, state.Inside ? state.MouseX : null, blend);
        }

        if (_perfSw is not null)
        {
            _perfWork += _perfSw.Elapsed.TotalMilliseconds;
            _perfSw = null;
        }

        if (!busy)
        {
            CompositionTarget.Rendering -= MagnifyFrame;
            _magnifyRunning = false;
        }
    }

    /// <summary>Lupe sofort zurücksetzen, z. B. bevor ein Symbol gezogen wird (Lücke braucht feste Größen).</summary>
    private void ResetMagnifyNow()
    {
        foreach (var state in _magnify.Values)
        {
            state.Inside = false;
        }

        ItemAnimations.ResetMagnify(ItemButtons(MainItemsControl), animate: false);
        ItemAnimations.ResetMagnify(ItemButtons(CategoryItemsControl), animate: false);
        UpdateLayout();
    }

    private static List<Button> ItemButtons(ItemsControl items)
    {
        var buttons = new List<Button>(items.Items.Count);
        foreach (object item in items.Items)
        {
            if (items.ItemContainerGenerator.ContainerFromItem(item) is DependencyObject container && FindButton(container) is { } button)
            {
                buttons.Add(button);
            }
        }

        return buttons;
    }

    private static Button? FindButton(DependencyObject parent)
    {
        if (parent is Button button)
        {
            return button;
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            if (FindButton(VisualTreeHelper.GetChild(parent, i)) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>Button eines Eintrags in Haupt- oder Kategorie-Dock.</summary>
    private Button? ButtonOf(ViewModels.DockItemViewModel item) =>
        (MainItemsControl.ItemContainerGenerator.ContainerFromItem(item) ?? CategoryItemsControl.ItemContainerGenerator.ContainerFromItem(item))
            is DependencyObject container ? FindButton(container) : null;
}
