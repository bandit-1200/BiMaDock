using System.Windows;
using System.Windows.Media;

namespace BiMaDock.Views;

/// <summary>
/// Zeichnet das Kategorie-Dock als Flüssigkeit: Ein Strahl tritt unter dem Kategorie-Symbol aus dem Hauptdock aus,
/// läuft als Tropfen nach unten, breitet sich zur Dock-Fläche aus und bleibt als schmaler Steg verbunden.
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

    /// <summary>Anteil des Ablaufs, in dem der Strahl nach unten läuft; danach breitet er sich aus.</summary>
    private const double StreamPhase = 0.34;

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

    /// <summary>Legt Austrittspunkt (Mitte unter dem Symbol, Unterkante Hauptdock) und Ziel-Fläche fest.</summary>
    public void Configure(Point source, double sourceWidth, Rect target, double cornerRadius)
    {
        _source = source;
        _sourceWidth = sourceWidth;
        _target = target;
        _cornerRadius = cornerRadius;
        InvalidateVisual();
    }

    /// <summary>Breite des Stegs im fertigen Zustand.</summary>
    public static double FinalNeckWidth => 16;

    protected override void OnRender(DrawingContext drawingContext)
    {
        double p = Math.Clamp(Progress, 0, 1);
        if (p <= 0 || _target.IsEmpty || _target.Width <= 0)
        {
            return;
        }

        double stream = EaseOutQuad(Math.Clamp(p / StreamPhase, 0, 1));
        double spread = Math.Clamp((p - (StreamPhase * 0.8)) / (1 - (StreamPhase * 0.8)), 0, 1);

        double cx = Math.Clamp(_source.X, _target.Left + _cornerRadius + 8, _target.Right - _cornerRadius - 8);
        double startNeck = Math.Max(_sourceWidth * 0.42, 22);
        double neck = Lerp(startNeck, FinalNeckWidth, EaseOut(spread));

        if (spread <= 0)
        {
            // Strahl mit Tropfen an der Spitze
            double dropRadius = neck * 0.62;
            // Tropfen quillt unter dem Hauptdock hervor und fällt bis zur Mitte des späteren Docks
            double bottom = Lerp(_source.Y + (dropRadius * 1.2), _target.Top + (_target.Height * 0.45), stream);
            Draw(drawingContext, ctx => AddStream(ctx, cx, _source.Y, bottom - dropRadius, neck, flare: 0));
            Draw(drawingContext, ctx => AddEllipse(ctx, new Point(cx, bottom - dropRadius), dropRadius * Lerp(0.6, 1, stream), dropRadius));
            return;
        }

        // Pfütze breitet sich aus und formt die Dock-Fläche; der Strahl wird zum Steg.
        double grow = EaseOutBack(spread, 0.9);
        double heightGrow = EaseOutBack(Math.Clamp(spread * 1.2, 0, 1), 0.6);
        double width = Lerp(neck * 1.24, _target.Width, grow);
        double height = Lerp(neck * 1.24, _target.Height, heightGrow);
        double centerX = Lerp(cx, _target.Left + (_target.Width / 2), EaseOut(spread));
        double top = Lerp(_target.Top + (_target.Height * 0.45) - (neck * 1.24), _target.Top, heightGrow);
        var body = new Rect(centerX - (width / 2), top, Math.Max(width, 1), Math.Max(height, 1));
        double radius = Math.Min(Lerp(height / 2, _cornerRadius, heightGrow), Math.Min(body.Width, body.Height) / 2);

        double neckX = Math.Clamp(cx, body.Left + radius, body.Right - radius);
        Draw(drawingContext, ctx => AddStream(ctx, neckX, _source.Y, body.Top, neck, flare: Lerp(2, 14, Math.Clamp(heightGrow, 0, 1))));
        Draw(drawingContext, ctx => AddRoundedRect(ctx, body, radius));
    }

    /// <summary>Jede Teilform einzeln und deckend zeichnen: Überlappungen bleiben so unsichtbar.</summary>
    private void Draw(DrawingContext drawingContext, Action<StreamGeometryContext> build)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            build(ctx);
        }

        geometry.Freeze();
        drawingContext.DrawGeometry(Fill, null, geometry);
    }

    /// <summary>Senkrechter Strahl mit eingezogenen Seiten; oben und unten weich ausgerundet (wie Wasser an einer Kante).</summary>
    private static void AddStream(StreamGeometryContext ctx, double cx, double top, double bottom, double width, double flare)
    {
        if (bottom <= top)
        {
            return;
        }

        double half = width / 2;
        double topHalf = half + Math.Min(16, (bottom - top) * 0.7); // weiche Ausrundung am Hauptdock
        double bottomHalf = half + flare;
        double h = bottom - top;
        top -= 8; // unter das Hauptdock reichen, damit keine Kante sichtbar wird

        ctx.BeginFigure(new Point(cx - topHalf, top), isFilled: true, isClosed: true);
        double waist = half * 0.82; // Strahl ist in der Mitte etwas schmaler, wie fließendes Wasser
        double mid = top + 8 + (h * 0.5);
        ctx.BezierTo(new Point(cx - half, top + 8), new Point(cx - waist, top + 8 + (h * 0.25)), new Point(cx - waist, mid), true, true);
        ctx.BezierTo(new Point(cx - waist, bottom - (h * 0.25)), new Point(cx - half, bottom - 2), new Point(cx - bottomHalf, bottom + 2), true, true);
        ctx.LineTo(new Point(cx + bottomHalf, bottom + 2), true, true);
        ctx.BezierTo(new Point(cx + half, bottom - 2), new Point(cx + waist, bottom - (h * 0.25)), new Point(cx + waist, mid), true, true);
        ctx.BezierTo(new Point(cx + waist, top + 8 + (h * 0.25)), new Point(cx + half, top + 8), new Point(cx + topHalf, top), true, true);
    }

    private static void AddEllipse(StreamGeometryContext ctx, Point center, double rx, double ry)
    {
        ctx.BeginFigure(new Point(center.X - rx, center.Y), true, true);
        ctx.ArcTo(new Point(center.X + rx, center.Y), new Size(rx, ry), 0, false, SweepDirection.Counterclockwise, true, true);
        ctx.ArcTo(new Point(center.X - rx, center.Y), new Size(rx, ry), 0, false, SweepDirection.Counterclockwise, true, true);
    }

    private static void AddRoundedRect(StreamGeometryContext ctx, Rect r, double radius)
    {
        var corner = new Size(radius, radius);
        ctx.BeginFigure(new Point(r.Left + radius, r.Top), true, true);
        ctx.LineTo(new Point(r.Right - radius, r.Top), true, true);
        ctx.ArcTo(new Point(r.Right, r.Top + radius), corner, 0, false, SweepDirection.Clockwise, true, true);
        ctx.LineTo(new Point(r.Right, r.Bottom - radius), true, true);
        ctx.ArcTo(new Point(r.Right - radius, r.Bottom), corner, 0, false, SweepDirection.Clockwise, true, true);
        ctx.LineTo(new Point(r.Left + radius, r.Bottom), true, true);
        ctx.ArcTo(new Point(r.Left, r.Bottom - radius), corner, 0, false, SweepDirection.Clockwise, true, true);
        ctx.LineTo(new Point(r.Left, r.Top + radius), true, true);
        ctx.ArcTo(new Point(r.Left + radius, r.Top), corner, 0, false, SweepDirection.Clockwise, true, true);
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    private static double EaseOut(double t) => 1 - Math.Pow(1 - t, 3);

    private static double EaseOutQuad(double t) => 1 - ((1 - t) * (1 - t));

    private static double EaseOutBack(double t, double overshoot)
    {
        double c3 = overshoot + 1;
        return 1 + (c3 * Math.Pow(t - 1, 3)) + (overshoot * Math.Pow(t - 1, 2));
    }
}
