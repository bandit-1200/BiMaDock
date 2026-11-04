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

        double width = MaxTextWidth + (2 * Pad);
        double height = Math.Ceiling(text.Height) + (2 * Pad);
        var visual = new DrawingVisual
        {
            Effect = new DropShadowEffect { BlurRadius = 4, ShadowDepth = 0, Opacity = 0.9, Color = Colors.Black },
        };
        using (var dc = visual.RenderOpen())
        {
            dc.DrawText(text, new Point(Pad, Pad));
        }

        var bitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(width * _dpi), (int)Math.Ceiling(height * _dpi), 96 * _dpi, 96 * _dpi, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        _image = bitmap;
    }
}
