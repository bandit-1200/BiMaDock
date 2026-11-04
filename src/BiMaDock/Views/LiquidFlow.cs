using System.Windows;
using System.Windows.Media;

namespace BiMaDock.Views;

/// <summary>
/// Zeichnet das Kategorie-Dock als Flüssigkeit:
/// 1. Ein Tropfen quillt unter dem Kategorie-Symbol aus dem Hauptdock und fällt nach unten.
/// 2. Beim Aufprall fließt das Wasser als flacher Strom mit runden Fronten nach links und rechts.
/// 3. Der Strom füllt sich zur vollen Höhe auf; Wellen laufen vom Aufprallpunkt nach außen und klingen ab.
/// 4. Der Strahl wird zum schmalen Steg, der mit dem Symbol verbunden bleibt.
/// <see cref="Progress"/> 0 = nichts, 1 = fertig geformt. Alle Koordinaten relativ zu diesem Element.
/// </summary>
internal sealed class LiquidFlow : FrameworkElement
{
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(LiquidFlow),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(LiquidFlow),
        new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ShowBodyProperty = DependencyProperty.Register(
        nameof(ShowBody), typeof(bool), typeof(LiquidFlow),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Anteil des Ablaufs, in dem der Tropfen fällt; danach fließt er auseinander.</summary>
    private const double FallPhase = 0.3;

    /// <summary>Breite des Stegs im fertigen Zustand.</summary>
    private const double FinalNeckWidth = 16;

    private Point _source;
    private double _sourceWidth;
    private Rect _target;
    private double _cornerRadius;

    public LiquidFlow()
    {
        IsHitTestVisible = false;
    }

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>
    /// False, sobald das echte Kategorie-Dock darüber liegt: dann nur noch den Steg zeichnen,
    /// damit sich halbtransparente Flächen nicht doppelt abdunkeln.
    /// </summary>
    public bool ShowBody
    {
        get => (bool)GetValue(ShowBodyProperty);
        set => SetValue(ShowBodyProperty, value);
    }

    // Nachfedern des Stegs: Die Waagerechte des Austrittspunkts folgt dem Symbol als gedämpfte Feder.
    // Oben (Unterkante Hauptdock) und unten (Kategorie-Dock) bleibt er senkrecht fest angeheftet.
    private const double SpringStiffness = 180; // je höher, desto straffer
    private const double SpringDamping = 0.65;  // < 1 schwingt leicht über, 1 = ohne Überschwingen

    private double _springX;
    private double _springVelocity;
    private double _targetX;
    private TimeSpan _lastFrame;
    private bool _springing;

    /// <summary>
    /// Legt Austrittspunkt (Mitte unter dem Symbol, Unterkante Hauptdock) und Ziel-Fläche fest.
    /// <paramref name="snap"/> setzt den Steg sofort (beim Öffnen), sonst federt er zur neuen Lage nach.
    /// </summary>
    public void Configure(Point source, double sourceWidth, Rect target, double cornerRadius, bool snap = false)
    {
        if (!snap && source == new Point(_targetX, _source.Y) && sourceWidth.Equals(_sourceWidth) && target == _target && cornerRadius.Equals(_cornerRadius))
        {
            return; // unverändert, kein Neuzeichnen
        }

        _targetX = source.X;
        if (snap)
        {
            _springX = source.X;
            _springVelocity = 0;
        }
        else if (!_springing && Math.Abs(_targetX - _springX) > 0.05)
        {
            _springing = true;
            _lastFrame = TimeSpan.Zero;
            CompositionTarget.Rendering += SpringFrame;
        }

        _source = new Point(_springX, source.Y);
        _sourceWidth = sourceWidth;
        _target = target;
        _cornerRadius = cornerRadius;
        InvalidateVisual();
    }

    private void SpringFrame(object? sender, EventArgs e)
    {
        var now = ((RenderingEventArgs)e).RenderingTime;
        if (now == _lastFrame)
        {
            return;
        }

        double dt = _lastFrame == TimeSpan.Zero ? 1.0 / 60 : Math.Min((now - _lastFrame).TotalSeconds, 0.04);
        _lastFrame = now;

        // halbimplizites Euler, in kleinen Schritten für Stabilität
        double damping = 2 * Math.Sqrt(SpringStiffness) * SpringDamping;
        for (double left = dt; left > 0; left -= 0.004)
        {
            double h = Math.Min(0.004, left);
            _springVelocity += ((SpringStiffness * (_targetX - _springX)) - (damping * _springVelocity)) * h;
            _springX += _springVelocity * h;
        }

        if (Math.Abs(_targetX - _springX) < 0.05 && Math.Abs(_springVelocity) < 0.5)
        {
            _springX = _targetX;
            _springVelocity = 0;
            _springing = false;
            CompositionTarget.Rendering -= SpringFrame;
        }

        _source = new Point(_springX, _source.Y);
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        double p = Math.Clamp(Progress, 0, 1);
        if (p <= 0 || _target.IsEmpty || _target.Width <= 0)
        {
            return;
        }

        double fall = EaseOutQuad(Math.Clamp(p / FallPhase, 0, 1));
        double spread = Math.Clamp((p - (FallPhase * 0.85)) / (1 - (FallPhase * 0.85)), 0, 1);

        double impactX = Math.Clamp(_source.X, _target.Left + _cornerRadius + 10, _target.Right - _cornerRadius - 10);
        double impactY = _target.Top + (_target.Height * 0.5);
        double startNeck = Math.Max(_sourceWidth * 0.42, 22);
        double neck = Lerp(startNeck, FinalNeckWidth, EaseOut(spread));

        Geometry shape;
        if (spread <= 0)
        {
            // Tropfen fällt; der Strahl reißt dabei nicht ab.
            double dropRadius = neck * 0.62;
            double bottom = Lerp(_source.Y + (dropRadius * 1.2), impactY + dropRadius, fall);
            var stream = Stream(impactX, _source.Y, bottom - dropRadius, neck, flare: 0);
            var drop = new EllipseGeometry(new Point(impactX, bottom - dropRadius), dropRadius * Lerp(0.6, 1, fall), dropRadius);
            shape = Geometry.Combine(stream, drop, GeometryCombineMode.Union, null);
        }
        else
        {
            // Erst seitlich als flacher Strom, dann auffüllen zur vollen Höhe.
            double widthGrow = EaseOutBack(Math.Clamp(spread * 1.35, 0, 1), 0.5);
            double heightGrow = EaseOutBack(Math.Clamp((spread - 0.3) / 0.7, 0, 1), 0.8);
            double thin = neck * 0.95;

            double left = Lerp(impactX - (thin / 2), _target.Left, widthGrow);
            double right = Lerp(impactX + (thin / 2), _target.Right, widthGrow);
            double height = Lerp(thin, _target.Height, heightGrow);
            double top = Lerp(impactY - (thin / 2), _target.Top, heightGrow);
            var body = new Rect(left, top, Math.Max(right - left, 1), Math.Max(height, 1));

            // Runde Fronten am Anfang, am Ende die Ecken des Docks
            double radius = Math.Min(Lerp(body.Height / 2, _cornerRadius, Math.Clamp(heightGrow, 0, 1)), Math.Min(body.Width, body.Height) / 2);

            // Wellen laufen vom Aufprall nach außen und klingen ab; Seiten wölben sich leicht.
            double calm = Math.Clamp(spread, 0, 1);
            // Wellenhöhe passt zur Dicke des Stroms: dünner Strom, flache Wellen
            double amplitude = Math.Min(4, body.Height * 0.09) * Math.Pow(1 - calm, 1.2) * Math.Min(1, calm * 6);
            double phase = p * 22;
            double bulge = 5 * (1 - calm) * Math.Min(1, calm * 4);

            double neckX = Math.Clamp(impactX, body.Left + radius, body.Right - radius);
            var stream = Stream(neckX, _source.Y, body.Top + 2, neck, flare: Lerp(2, 14, Math.Clamp(heightGrow, 0, 1)));
            shape = ShowBody
                ? Geometry.Combine(stream, Body(body, radius, impactX, amplitude, phase, bulge), GeometryCombineMode.Union, null)
                : stream;
        }

        shape.Freeze();
        drawingContext.DrawGeometry(Fill, null, shape);
    }

    /// <summary>Senkrechter Strahl, in der Mitte leicht eingeschnürt, oben und unten weich ausgerundet.</summary>
    private static StreamGeometry Stream(double cx, double top, double bottom, double width, double flare)
    {
        var geometry = new StreamGeometry();
        if (bottom <= top)
        {
            return geometry;
        }

        using var ctx = geometry.Open();
        double half = width / 2;
        double waist = half * 0.82;
        double topHalf = half + Math.Min(12, (bottom - top) * 0.45); // feste Ausrundung, wackelt nicht mit der Länge
        double bottomHalf = half + flare;
        double h = bottom - top;
        top -= 1; // bündig an die Unterkante des Hauptdocks
        double mid = top + (h * 0.5);

        ctx.BeginFigure(new Point(cx - topHalf, top), isFilled: true, isClosed: true);
        ctx.BezierTo(new Point(cx - half, top), new Point(cx - waist, top + (h * 0.2)), new Point(cx - waist, mid), true, true);
        ctx.BezierTo(new Point(cx - waist, bottom - (h * 0.25)), new Point(cx - half, bottom - 2), new Point(cx - bottomHalf, bottom + 2), true, true);
        ctx.LineTo(new Point(cx + bottomHalf, bottom + 2), true, true);
        ctx.BezierTo(new Point(cx + half, bottom - 2), new Point(cx + waist, bottom - (h * 0.25)), new Point(cx + waist, mid), true, true);
        ctx.BezierTo(new Point(cx + waist, top + (h * 0.2)), new Point(cx + half, top), new Point(cx + topHalf, top), true, true);
        return geometry;
    }

    /// <summary>Abgerundete Fläche, deren Ober- und Unterkante Wellen tragen und deren Seiten sich wölben.</summary>
    private static StreamGeometry Body(Rect r, double radius, double impactX, double amplitude, double phase, double bulge)
    {
        const double Step = 3;
        const int ArcSteps = 8;
        var points = new List<Point>(256);

        double Wave(double x)
        {
            double edge = Math.Min(x - (r.Left + radius), (r.Right - radius) - x);
            double taper = SmoothStep(Math.Clamp(edge / 18, 0, 1));
            return amplitude * taper * Math.Sin((2 * Math.PI * Math.Abs(x - impactX) / 64) - phase);
        }

        double sideSpan = r.Height - (2 * radius);
        double Bulge(double y) => sideSpan <= 0 ? 0 : bulge * Math.Sin(Math.PI * (y - r.Top - radius) / sideSpan);

        void Arc(double cx, double cy, double fromDeg, double toDeg)
        {
            for (int i = 0; i <= ArcSteps; i++)
            {
                double a = (fromDeg + ((toDeg - fromDeg) * i / ArcSteps)) * Math.PI / 180;
                points.Add(new Point(cx + (radius * Math.Cos(a)), cy + (radius * Math.Sin(a))));
            }
        }

        for (double x = r.Left + radius; x <= r.Right - radius; x += Step)
        {
            points.Add(new Point(x, r.Top - Wave(x)));
        }

        Arc(r.Right - radius, r.Top + radius, -90, 0);
        for (double y = r.Top + radius; y <= r.Bottom - radius; y += Step)
        {
            points.Add(new Point(r.Right + Bulge(y), y));
        }

        Arc(r.Right - radius, r.Bottom - radius, 0, 90);
        for (double x = r.Right - radius; x >= r.Left + radius; x -= Step)
        {
            points.Add(new Point(x, r.Bottom + (Wave(x) * 0.6)));
        }

        Arc(r.Left + radius, r.Bottom - radius, 90, 180);
        for (double y = r.Bottom - radius; y >= r.Top + radius; y -= Step)
        {
            points.Add(new Point(r.Left - Bulge(y), y));
        }

        Arc(r.Left + radius, r.Top + radius, 180, 270);

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(points[0], isFilled: true, isClosed: true);
            ctx.PolyLineTo(points.Skip(1).ToList(), isStroked: false, isSmoothJoin: true);
        }

        return geometry;
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    private static double SmoothStep(double t) => t * t * (3 - (2 * t));

    private static double EaseOut(double t) => 1 - Math.Pow(1 - t, 3);

    private static double EaseOutQuad(double t) => 1 - ((1 - t) * (1 - t));

    private static double EaseOutBack(double t, double overshoot)
    {
        double c3 = overshoot + 1;
        return 1 + (c3 * Math.Pow(t - 1, 3)) + (overshoot * Math.Pow(t - 1, 2));
    }
}
