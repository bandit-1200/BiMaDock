using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BiMaDock.Views;

/// <summary>
/// Waagerechter Sichtbereich für eine Symbolleiste. Passt alles hinein, verhält er sich wie ein normales Panel.
/// Ist die Leiste breiter als <see cref="MaxViewportWidth"/>, wird sie seitlich verschoben (<see cref="Offset"/>)
/// und nur links und rechts abgeschnitten, nicht oben und unten, damit Hüpfen und Lupe frei bleiben.
/// </summary>
[ContentProperty(nameof(Child))]
public sealed class DockScrollPanel : FrameworkElement
{
    public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(
        nameof(Offset), typeof(double), typeof(DockScrollPanel),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange));

    private UIElement? _child;
    private double _maxViewport = double.PositiveInfinity;
    private (bool, bool, bool) _lastState = (false, true, true);

    /// <summary>Meldet, ob gescrollt werden kann und ob am Anfang oder Ende angekommen.</summary>
    public event EventHandler? ScrollStateChanged;

    public UIElement? Child
    {
        get => _child;
        set
        {
            if (_child is not null)
            {
                RemoveVisualChild(_child);
                RemoveLogicalChild(_child);
            }

            _child = value;
            if (_child is not null)
            {
                AddVisualChild(_child);
                AddLogicalChild(_child);
            }

            InvalidateMeasure();
        }
    }

    public double MaxViewportWidth
    {
        get => _maxViewport;
        set
        {
            _maxViewport = Math.Max(100, value);
            InvalidateMeasure();
        }
    }

    public double Offset
    {
        get => (double)GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    public double ExtentWidth { get; private set; }

    public double MaxOffset => Math.Max(0, ExtentWidth - ActualWidth);

    public bool CanScroll => MaxOffset > 0.5;

    public bool AtStart => ClampedOffset <= 0.5;

    public bool AtEnd => ClampedOffset >= MaxOffset - 0.5;

    private double ClampedOffset => Math.Clamp(Offset, 0, MaxOffset);

    /// <summary>Weich um <paramref name="delta"/> Pixel scrollen.</summary>
    public void ScrollBy(double delta, int ms = 260) => ScrollTo(ClampedOffset + delta, ms);

    public void ScrollTo(double offset, int ms = 260)
    {
        double target = Math.Clamp(offset, 0, MaxOffset);
        var animation = new DoubleAnimation(ClampedOffset, target, TimeSpan.FromMilliseconds(ms))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop,
        };
        animation.Completed += (_, _) => Offset = target;
        Offset = target;
        BeginAnimation(OffsetProperty, ms <= 0 ? null : animation);
    }

    /// <summary>Ohne Animation verschieben (für Dauerscrollen pro Bild).</summary>
    public void Nudge(double delta)
    {
        BeginAnimation(OffsetProperty, null);
        Offset = Math.Clamp(ClampedOffset + delta, 0, MaxOffset);
    }

    /// <summary>Sorgt dafür, dass der Bereich [left, right] (in Kind-Koordinaten ohne Verschiebung) sichtbar ist.</summary>
    public void BringIntoView(double left, double right)
    {
        if (left < ClampedOffset)
        {
            ScrollTo(left - 8);
        }
        else if (right > ClampedOffset + ActualWidth)
        {
            ScrollTo(right - ActualWidth + 8);
        }
    }

    protected override int VisualChildrenCount => _child is null ? 0 : 1;

    protected override Visual GetVisualChild(int index) => _child ?? throw new ArgumentOutOfRangeException(nameof(index));

    protected override System.Collections.IEnumerator LogicalChildren =>
        (_child is null ? Array.Empty<object>() : new object[] { _child }).GetEnumerator();

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_child is null)
        {
            return default;
        }

        _child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        var desired = _child.DesiredSize;
        double limit = Math.Min(_maxViewport, availableSize.Width);
        return new Size(Math.Min(desired.Width, limit), desired.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_child is null)
        {
            return finalSize;
        }

        ExtentWidth = _child.DesiredSize.Width;
        double offset = Math.Clamp(Offset, 0, Math.Max(0, ExtentWidth - finalSize.Width));
        _child.Arrange(new Rect(-offset, 0, Math.Max(ExtentWidth, finalSize.Width), finalSize.Height));

        // Nur seitlich abschneiden; oben und unten großzügig Platz lassen.
        Clip = ExtentWidth > finalSize.Width + 0.5
            ? new RectangleGeometry(new Rect(0, -400, finalSize.Width, finalSize.Height + 800))
            : null;

        var state = (ExtentWidth > finalSize.Width + 0.5, offset <= 0.5, offset >= ExtentWidth - finalSize.Width - 0.5);
        if (state != _lastState)
        {
            _lastState = state;
            Dispatcher.BeginInvoke(() => ScrollStateChanged?.Invoke(this, EventArgs.Empty));
        }

        return finalSize;
    }
}
