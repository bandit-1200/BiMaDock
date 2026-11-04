using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BiMaDock.Core.Settings;
using BiMaDock.ViewModels;

namespace BiMaDock.Views;

/// <summary>
/// Hover- und Start-Animationen der Dock-Symbole. Wird über <see cref="EnabledProperty"/> im Button-Stil
/// eingehängt; der gewählte Effekt kommt aus den Einstellungen (<see cref="Configure"/>).
/// </summary>
public static class ItemAnimations
{
    private static HoverEffect _effect = HoverEffect.Zoom;
    private static bool _launchBounce = true;

    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(ItemAnimations), new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject d) => (bool)d.GetValue(EnabledProperty);

    public static void SetEnabled(DependencyObject d, bool value) => d.SetValue(EnabledProperty, value);

    public static void Configure(AppSettings settings)
    {
        _effect = settings.HoverEffect;
        _launchBounce = settings.LaunchBounce;
    }

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Button button || e.NewValue is not true)
        {
            return;
        }

        button.MouseEnter += (_, _) => { if (Parts(button) is { } p) StartHover(p, _effect); };
        button.MouseLeave += (_, _) => { if (Parts(button) is { } p) EndHover(p); };
        button.Click += (_, _) =>
        {
            if (_launchBounce && button.DataContext is DockItemViewModel { IsCategory: false } && Parts(button) is { } p)
            {
                Bounce(p);
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

        var group = new TransformGroup();
        var parts = new IconParts(new ScaleTransform(), new RotateTransform(), new TranslateTransform(), icon);
        group.Children.Add(parts.Scale);
        group.Children.Add(parts.Rotate);
        group.Children.Add(parts.Translate);
        icon.RenderTransformOrigin = new Point(0.5, 0.5);
        icon.RenderTransform = group;
        return parts;
    }

    private static void StartHover(IconParts p, HoverEffect effect)
    {
        p.Icon.RenderTransformOrigin = new Point(0.5, 0.5);
        switch (effect)
        {
            case HoverEffect.Zoom:
                Scale(p, To(1.16, 160, new CubicEase { EasingMode = EasingMode.EaseOut }));
                break;

            case HoverEffect.Hop:
                Scale(p, To(1.08, 160, null));
                p.Translate.BeginAnimation(TranslateTransform.YProperty, Keys(420, (0, 0), (0.35, -12), (0.7, 0), (0.85, -3), (1, 0)));
                break;

            case HoverEffect.Swing:
                p.Icon.RenderTransformOrigin = new Point(0.5, 0.05);
                p.Rotate.BeginAnimation(RotateTransform.AngleProperty,
                    Keys(1100, (0, 0), (0.15, 14), (0.35, -10), (0.55, 6), (0.75, -3), (0.9, 1), (1, 0)));
                break;

            case HoverEffect.Wiggle:
                p.Rotate.BeginAnimation(RotateTransform.AngleProperty,
                    Keys(380, (0, 0), (0.2, -8), (0.45, 7), (0.7, -4), (0.85, 2), (1, 0)));
                break;

            case HoverEffect.Pulse:
                var pulse = Keys(1000, (0, 1), (0.5, 1.12), (1, 1));
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
    private static void Bounce(IconParts p) =>
        p.Translate.BeginAnimation(TranslateTransform.YProperty,
            Keys(700, (0, 0), (0.22, -18), (0.45, 0), (0.62, -8), (0.8, 0), (0.9, -2), (1, 0)));

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
