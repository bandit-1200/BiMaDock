using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BiMaDock.Core.Settings;
using BiMaDock.ViewModels;

namespace BiMaDock.Views;

/// <summary>
/// Animationen der Dock-Symbole: Hover-Effekt, Hüpfen beim Starten, Lupe, Hereinploppen und Wegschrumpfen.
/// Wird über <see cref="EnabledProperty"/> im Button-Stil eingehängt; Werte kommen aus den Einstellungen.
/// </summary>
public static class ItemAnimations
{
    /// <summary>Grundgröße eines Symbols und seiner Fläche (siehe DockItemButtonStyle).</summary>
    public const double IconSize = 44;
    public const double SlotSize = 64;

    private static AppSettings _settings = new();

    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(ItemAnimations), new PropertyMetadata(false, OnEnabledChanged));

    /// <summary>Lupenfaktor eines Symbols (1 = normal). Animierbar; passt Breite und Höhe der Fläche an.</summary>
    public static readonly DependencyProperty MagnificationProperty = DependencyProperty.RegisterAttached(
        "Magnification", typeof(double), typeof(ItemAnimations), new PropertyMetadata(1.0, OnMagnificationChanged));

    public static bool GetEnabled(DependencyObject d) => (bool)d.GetValue(EnabledProperty);

    public static void SetEnabled(DependencyObject d, bool value) => d.SetValue(EnabledProperty, value);

    public static double GetMagnification(DependencyObject d) => (double)d.GetValue(MagnificationProperty);

    public static void SetMagnification(DependencyObject d, double value) => d.SetValue(MagnificationProperty, value);

    public static AppSettings Settings => _settings;

    public static void Configure(AppSettings settings) => _settings = settings;

    // ---------- Einhängen ----------

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Button button || e.NewValue is not true)
        {
            return;
        }

        button.MouseEnter += (_, _) => { if (Parts(button) is { } p) StartHover(p); };
        button.MouseLeave += (_, _) => { if (Parts(button) is { } p) EndHover(p); };
        button.Click += (_, _) =>
        {
            if (_settings.LaunchBounce && button.DataContext is DockItemViewModel { IsCategory: false } && Parts(button) is { } p)
            {
                Bounce(p);
            }
        };
        button.Loaded += (_, _) =>
        {
            if (button.DataContext is DockItemViewModel { IsNew: true } item)
            {
                item.IsNew = false;
                if (_settings.ItemPopEnabled)
                {
                    PopIn(button);
                }
            }
        };
    }

    private sealed record IconParts(ScaleTransform Scale, RotateTransform Rotate, TranslateTransform Translate, FrameworkElement Icon);

    private static IconParts? Parts(Button button)
    {
        if (button.Template?.FindName("IconImage", button) is not FrameworkElement icon)
        {
            return null;
        }

        if (icon.RenderTransform is TransformGroup { Children: [ScaleTransform s, RotateTransform r, TranslateTransform t] })
        {
            return new IconParts(s, r, t, icon);
        }

        var parts = new IconParts(new ScaleTransform(), new RotateTransform(), new TranslateTransform(), icon);
        var group = new TransformGroup();
        group.Children.Add(parts.Scale);
        group.Children.Add(parts.Rotate);
        group.Children.Add(parts.Translate);
        icon.RenderTransformOrigin = new Point(0.5, 0.5);
        icon.RenderTransform = group;
        return parts;
    }

    // ---------- Hover ----------

    private static double Strength => _settings.HoverIntensity / 100.0;

    private static int Ms(int baseMs) => (int)Math.Round(baseMs * 100.0 / _settings.HoverSpeed);

    private static void StartHover(IconParts p)
    {
        double k = Strength;
        p.Rotate.CenterY = 0;
        switch (_settings.HoverEffect)
        {
            case HoverEffect.Zoom when !_settings.MagnifyEnabled:
                Scale(p, To(1 + (0.16 * k), Ms(160), new CubicEase { EasingMode = EasingMode.EaseOut }));
                break;

            case HoverEffect.Hop:
                p.Translate.BeginAnimation(TranslateTransform.YProperty,
                    Keys(Ms(420), (0, 0), (0.35, -12 * k), (0.7, 0), (0.85, -3 * k), (1, 0)));
                break;

            case HoverEffect.Swing:
                p.Rotate.CenterY = -(p.Icon.ActualHeight / 2) + 2; // hängt oben
                p.Rotate.BeginAnimation(RotateTransform.AngleProperty,
                    Keys(Ms(1100), (0, 0), (0.15, 14 * k), (0.35, -10 * k), (0.55, 6 * k), (0.75, -3 * k), (0.9, k), (1, 0)));
                break;

            case HoverEffect.Wiggle:
                p.Rotate.BeginAnimation(RotateTransform.AngleProperty,
                    Keys(Ms(380), (0, 0), (0.2, -8 * k), (0.45, 7 * k), (0.7, -4 * k), (0.85, 2 * k), (1, 0)));
                break;

            case HoverEffect.Pulse:
                var pulse = Keys(Ms(1000), (0, 1), (0.5, 1 + (0.12 * k)), (1, 1));
                pulse.RepeatBehavior = RepeatBehavior.Forever;
                Scale(p, pulse);
                break;
        }
    }

    private static void EndHover(IconParts p)
    {
        Scale(p, To(1, 200, new CubicEase { EasingMode = EasingMode.EaseOut }));
        p.Translate.BeginAnimation(TranslateTransform.YProperty, To(0, 200, null));
        p.Rotate.BeginAnimation(RotateTransform.AngleProperty, To(0, 250, null));
    }

    /// <summary>Kurzes Hüpfen beim Starten, wie beim Mac.</summary>
    private static void Bounce(IconParts p)
    {
        double h = _settings.LaunchBounceHeight;
        p.Translate.BeginAnimation(TranslateTransform.YProperty,
            Keys(700, (0, 0), (0.22, -h), (0.45, 0), (0.62, -h * 0.45), (0.8, 0), (0.9, -h * 0.12), (1, 0)));
    }

    // ---------- Lupe ----------

    private static void OnMagnificationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Button button || button.Template is null)
        {
            return;
        }

        double f = (double)e.NewValue;
        double extra = IconSize * (f - 1);
        button.Width = SlotSize + extra;
        if (button.Template.FindName("IconArea", button) is FrameworkElement area)
        {
            area.Height = SlotSize + extra;
        }

        if (button.Template.FindName("IconImage", button) is FrameworkElement icon)
        {
            icon.Width = IconSize * f;
            icon.Height = IconSize * f;
        }
    }

    /// <summary>
    /// Setzt die Lupe für alle Symbole einer Leiste abhängig von der Mausposition (x relativ zu <paramref name="panel"/>).
    /// <paramref name="smooth"/> blendet weich über (Eintritt in die Leiste).
    /// </summary>
    public static void Magnify(IEnumerable<Button> buttons, FrameworkElement panel, double mouseX, bool smooth)
    {
        double max = _settings.MagnifyScale / 100.0;
        double reach = (_settings.MagnifyRange + 0.6) * SlotSize;
        foreach (var button in buttons)
        {
            double center = button.TranslatePoint(new Point(button.ActualWidth / 2, 0), panel).X;
            double t = Math.Max(0, 1 - (Math.Abs(mouseX - center) / reach));
            double f = 1 + ((max - 1) * (t * t * (3 - (2 * t)))); // weicher Abfall (smoothstep)
            if (smooth)
            {
                button.BeginAnimation(MagnificationProperty, To(f, 110, new CubicEase { EasingMode = EasingMode.EaseOut }));
            }
            else
            {
                button.BeginAnimation(MagnificationProperty, null);
                SetMagnification(button, f);
            }
        }
    }

    /// <summary>Lupe zurücknehmen, weich oder sofort (z. B. vor dem Ziehen).</summary>
    public static void ResetMagnify(IEnumerable<Button> buttons, bool animate)
    {
        foreach (var button in buttons)
        {
            if (animate)
            {
                button.BeginAnimation(MagnificationProperty, To(1, 180, new CubicEase { EasingMode = EasingMode.EaseOut }));
            }
            else
            {
                button.BeginAnimation(MagnificationProperty, null);
                SetMagnification(button, 1);
            }
        }
    }

    // ---------- Hinzufügen und Entfernen ----------

    private static ScaleTransform ButtonScale(Button button)
    {
        if (button.RenderTransform is not ScaleTransform scale || scale.IsFrozen)
        {
            scale = new ScaleTransform(1, 1);
            button.RenderTransformOrigin = new Point(0.5, 0.4);
            button.RenderTransform = scale;
        }

        return scale;
    }

    /// <summary>Neuer Eintrag ploppt herein: Platz öffnet sich, Symbol wächst mit kleinem Überschwingen.</summary>
    public static void PopIn(Button button)
    {
        int ms = _settings.ItemPopDurationMs;
        var scale = ButtonScale(button);
        var grow = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(ms)) { EasingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut } };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);

        var width = new DoubleAnimation(0, SlotSize, TimeSpan.FromMilliseconds(ms * 0.7)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        width.Completed += (_, _) => button.BeginAnimation(FrameworkElement.WidthProperty, null);
        button.BeginAnimation(FrameworkElement.WidthProperty, width);
    }

    /// <summary>Eintrag schrumpft weg, die Nachbarn rücken zusammen; danach wird <paramref name="done"/> aufgerufen.</summary>
    public static void ShrinkOut(Button button, Action done)
    {
        if (!_settings.ItemPopEnabled)
        {
            done();
            return;
        }

        int ms = _settings.ItemPopDurationMs;
        var scale = ButtonScale(button);
        var shrink = new DoubleAnimation(0, TimeSpan.FromMilliseconds(ms * 0.6)) { EasingFunction = new BackEase { Amplitude = 0.4, EasingMode = EasingMode.EaseIn } };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);

        var width = new DoubleAnimation(button.ActualWidth, 0, TimeSpan.FromMilliseconds(ms)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } };
        width.Completed += (_, _) => done();
        button.BeginAnimation(FrameworkElement.WidthProperty, width);
    }

    // ---------- Hilfen ----------

    private static void Scale(IconParts p, AnimationTimeline animation)
    {
        p.Scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
        p.Scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
    }

    private static DoubleAnimation To(double to, int ms, IEasingFunction? ease) =>
        new(to, TimeSpan.FromMilliseconds(ms)) { EasingFunction = ease };

    private static DoubleAnimationUsingKeyFrames Keys(int ms, params (double At, double Value)[] frames)
    {
        var animation = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(ms) };
        var ease = new SineEase { EasingMode = EasingMode.EaseInOut };
        foreach (var (at, value) in frames)
        {
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(value, KeyTime.FromPercent(at), ease));
        }

        return animation;
    }
}
