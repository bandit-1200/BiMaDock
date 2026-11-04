using System.Windows;
using System.Windows.Media;

namespace BiMaDock.Views;

/// <summary>
/// Weicher Schatten unter einer Dock-Fläche, gezeichnet aus wenigen halbtransparenten, nach außen größer
/// werdenden abgerundeten Flächen. Sieht aus wie ein DropShadowEffect, braucht aber keinen Weichzeichner,
/// der bei jeder Größenänderung (Lupe) die ganze Fläche neu berechnen müsste.
/// </summary>
public sealed class SoftShadow : FrameworkElement
{
    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius), typeof(CornerRadius), typeof(SoftShadow),
        new FrameworkPropertyMetadata(new CornerRadius(18), FrameworkPropertyMetadataOptions.AffectsRender));

    private const int Layers = 9;
    private const double Spread = 13;     // wie weit der Schatten nach außen reicht
    private const double OffsetY = 4;     // nach unten versetzt wie vorher
    private const double TotalAlpha = 0.45;

    private static readonly Brush[] LayerBrushes = CreateBrushes();

    public SoftShadow()
    {
        IsHitTestVisible = false;
    }

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var r = CornerRadius;
        for (int i = 0; i < Layers; i++)
        {
            // außen die größte, schwächste Fläche; innen kleiner. Übereinander ergibt das einen weichen Verlauf.
            double grow = Spread * (1 - ((double)i / Layers));
            var rect = new Rect(-grow, OffsetY - grow, ActualWidth + (2 * grow), ActualHeight + (2 * grow));
            drawingContext.DrawGeometry(LayerBrushes[i], null, Rounded(rect, r, grow));
        }
    }

    private static Brush[] CreateBrushes()
    {
        var brushes = new Brush[Layers];
        double perLayer = 1 - Math.Pow(1 - TotalAlpha, 1.0 / Layers);
        for (int i = 0; i < Layers; i++)
        {
            var brush = new SolidColorBrush(Color.FromArgb((byte)Math.Round(perLayer * 255), 0, 0, 0));
            brush.Freeze();
            brushes[i] = brush;
        }

        return brushes;
    }

    /// <summary>Rechteck mit je Ecke eigenem Radius; Ecken mit Radius 0 bekommen den Zuwachs als Rundung.</summary>
    private static StreamGeometry Rounded(Rect rect, CornerRadius radius, double grow)
    {
        double tl = radius.TopLeft + grow, tr = radius.TopRight + grow, br = radius.BottomRight + grow, bl = radius.BottomLeft + grow;
        double max = Math.Min(rect.Width, rect.Height) / 2;
        tl = Math.Min(tl, max);
        tr = Math.Min(tr, max);
        br = Math.Min(br, max);
        bl = Math.Min(bl, max);

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(rect.Left + tl, rect.Top), isFilled: true, isClosed: true);
            ctx.LineTo(new Point(rect.Right - tr, rect.Top), false, false);
            ctx.ArcTo(new Point(rect.Right, rect.Top + tr), new Size(tr, tr), 0, false, SweepDirection.Clockwise, false, false);
            ctx.LineTo(new Point(rect.Right, rect.Bottom - br), false, false);
            ctx.ArcTo(new Point(rect.Right - br, rect.Bottom), new Size(br, br), 0, false, SweepDirection.Clockwise, false, false);
            ctx.LineTo(new Point(rect.Left + bl, rect.Bottom), false, false);
            ctx.ArcTo(new Point(rect.Left, rect.Bottom - bl), new Size(bl, bl), 0, false, SweepDirection.Clockwise, false, false);
            ctx.LineTo(new Point(rect.Left, rect.Top + tl), false, false);
            ctx.ArcTo(new Point(rect.Left + tl, rect.Top), new Size(tl, tl), 0, false, SweepDirection.Clockwise, false, false);
        }

        geometry.Freeze();
        return geometry;
    }
}
