using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace BiMaDock.Views;

/// <summary>
/// Beschriftung unter einem Symbol mit weichem Schatten. Text und Schatten werden einmal in ein Bild gerechnet;
/// danach kostet die Beschriftung beim Zeichnen fast nichts, auch wenn sich die Lupe bewegt (ein DropShadowEffect
/// pro Beschriftung würde bei vielen Symbolen jedes Bild spürbar verlangsamen).
/// </summary>
public sealed class ShadowLabel : FrameworkElement
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(ShadowLabel),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure, (d, _) => ((ShadowLabel)d)._image = null));

    private const double FontSize = 11;
    private const double MaxTextWidth = 60;
    private const double Pad = 3;

    private ImageSource? _image;
    private double _dpi = 1;

    public ShadowLabel()
    {
        IsHitTestVisible = false;

        // Das Bild liegt 1:1 auf den Bildschirmpixeln: auf Pixelraster einrasten, nicht neu abtasten (sonst unscharf)
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        _image = null;
        InvalidateMeasure();
        base.OnDpiChanged(oldDpi, newDpi);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        EnsureImage();
        return _image is null ? default : new Size(_image.Width, _image.Height);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        EnsureImage();
        if (_image is not null)
        {
            drawingContext.DrawImage(_image, new Rect(0, 0, _image.Width, _image.Height));
        }
    }

    private void EnsureImage()
    {
        if (_image is not null || string.IsNullOrEmpty(Text))
        {
            return;
        }

        _dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var text = new FormattedText(Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
            FontSize, new SolidColorBrush(Color.FromArgb(0xF2, 0xFF, 0xFF, 0xFF)), _dpi)
        {
            MaxTextWidth = MaxTextWidth,
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis,
            TextAlignment = TextAlignment.Center,
        };

        // Alles in ganzen Gerätepixeln rechnen: ein Rand von 3 DIP wären bei 125 % 3,75 px, der Text läge dann
        // zwischen zwei Pixeln und würde weichgezeichnet.
        int padPx = (int)Math.Ceiling(Pad * _dpi);
        int widthPx = (int)Math.Ceiling(MaxTextWidth * _dpi) + (2 * padPx);
        int heightPx = (int)Math.Ceiling(text.Height * _dpi) + (2 * padPx);
        var visual = new DrawingVisual
        {
            Effect = new DropShadowEffect { BlurRadius = 4, ShadowDepth = 0, Opacity = 0.9, Color = Colors.Black },
        };

        // FormattedText nimmt die Einstellungen des Fensters nicht an: Anzeige-Modus (pixelgenau ausgerichtet)
        // und Graustufen (ClearType gibt es auf durchsichtigem Untergrund nicht) ausdrücklich setzen.
        TextOptions.SetTextFormattingMode(visual, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(visual, TextRenderingMode.Grayscale);
        using (var dc = visual.RenderOpen())
        {
            dc.DrawText(text, new Point(padPx / _dpi, padPx / _dpi));
        }

        var bitmap = new RenderTargetBitmap(widthPx, heightPx, 96 * _dpi, 96 * _dpi, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        _image = bitmap;
    }
}
