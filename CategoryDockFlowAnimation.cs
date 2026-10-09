using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BiMaDock;

/// <summary>
/// "Herausfließen" der Kategoriebar aus dem angeklickten Kategorie-Element (ähnlich einem macOS-Stapel):
/// Die Bar wächst aus dem Element heraus, die Elemente gleiten zeitversetzt von dort an ihren Platz.
/// Animiert werden ausschließlich Render-Eigenschaften (RenderTransform, Opacity), nie das Layout,
/// damit Positionierung, Einfügeposition und Treffertests beim Ziehen unverändert bleiben.
/// Alle Animationen enden mit FillBehavior.Stop auf dem Ruhewert (Identität, Deckkraft 1).
/// </summary>
internal static class CategoryDockFlowAnimation
{
    internal const int MinMilliseconds = 150;
    internal const int MaxMilliseconds = 800;
    internal const int DefaultMilliseconds = 300;

    private const double SimpleFadeMilliseconds = 150;
    private const double DockStartScale = 0.3;
    private const double ItemStartScale = 0.4;
    private const double ItemDurationShare = 0.6;   // Anteil eines Elements an der Gesamtdauer
    private const double MaxStaggerMilliseconds = 40;

    // Verwirft verspätete Start-Aufrufe, wenn die Bar zwischenzeitlich neu aufgebaut oder geschlossen wurde.
    private static int generation;

    /// <summary>
    /// Synchron vor dem Einreihen der Positionierung aufrufen: stoppt laufende Animationen und blendet die
    /// Bar unsichtbar, damit sie nicht für ein Bild an der Endposition erscheint.
    /// </summary>
    public static int Prepare(Border dock, Panel items)
    {
        Reset(dock, items);
        dock.Opacity = 0;
        return generation;
    }

    /// <summary>Stoppt alle Animationen sofort und stellt den Ruhezustand her (z. B. beim Schließen).</summary>
    public static void Reset(Border dock, Panel items)
    {
        generation++;

        dock.BeginAnimation(UIElement.OpacityProperty, null);
        dock.Opacity = 1;
        if (dock.RenderTransform is ScaleTransform scale)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            scale.ScaleX = 1;
            scale.ScaleY = 1;
        }

        items.BeginAnimation(UIElement.OpacityProperty, null);
        foreach (var button in items.Children.OfType<Button>())
        {
            StopItem(button);
        }
    }

    /// <summary>
    /// Startet die Animation nach der Positionierung (Dispatcher-Priorität Loaded).
    /// </summary>
    /// <param name="origin">Kategorie-Element im Hauptdock; ohne Element nur Einblenden.</param>
    /// <param name="glideItems">Elemente einzeln gleiten lassen (beim Ziehen aus, damit die Einfügeposition stabil bleibt).</param>
    public static void Play(int token, Border dock, Panel items, FrameworkElement? origin,
        bool enabled, int milliseconds, bool glideItems)
    {
        if (token != generation)
        {
            return; // Inzwischen neu aufgebaut oder geschlossen
        }

        if (dock.Visibility != Visibility.Visible)
        {
            Reset(dock, items);
            return;
        }

        // Die Positionierung hat gerade den Rand geändert: Layout aktualisieren, bevor Punkte umgerechnet werden.
        dock.UpdateLayout();

        double totalMs = Math.Clamp(milliseconds, MinMilliseconds, MaxMilliseconds);
        if (!enabled || origin == null || !origin.IsVisible || dock.ActualWidth <= 0
            || !TryGetOrigin(origin, dock, out Point originPoint))
        {
            AnimateDockOpacity(dock, SimpleFadeMilliseconds);
            return;
        }

        var scale = dock.RenderTransform as ScaleTransform;
        if (scale == null || scale.IsFrozen)
        {
            scale = new ScaleTransform(1, 1);
            dock.RenderTransform = scale;
        }
        scale.CenterX = originPoint.X;
        scale.CenterY = originPoint.Y;

        var scaleAnimation = CreateAnimation(DockStartScale, 1, 0, totalMs, new BackEase { Amplitude = 0.25, EasingMode = EasingMode.EaseOut });
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation, HandoffBehavior.SnapshotAndReplace);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation, HandoffBehavior.SnapshotAndReplace);
        AnimateDockOpacity(dock, totalMs * 0.5);

        if (!glideItems)
        {
            return;
        }

        var buttons = items.Children.OfType<Button>()
            .Where(button => button.Visibility == Visibility.Visible && button.ActualWidth > 0)
            .ToList();
        double itemMs = GetItemDuration(totalMs);
        for (int index = 0; index < buttons.Count; index++)
        {
            GlideItem(buttons[index], dock, originPoint, GetItemDelay(index, buttons.Count, totalMs), itemMs);
        }
    }

    /// <summary>Dauer der Gleitbewegung eines einzelnen Elements.</summary>
    internal static double GetItemDuration(double totalMilliseconds) =>
        Math.Max(0, totalMilliseconds) * ItemDurationShare;

    /// <summary>
    /// Verzögerung des Elements <paramref name="index"/>. Der Versatz schrumpft bei vielen Elementen,
    /// sodass das letzte Element spätestens nach der Gesamtdauer an seinem Platz ist.
    /// </summary>
    internal static double GetItemDelay(int index, int count, double totalMilliseconds)
    {
        if (count <= 1 || index <= 0)
        {
            return 0;
        }

        double available = Math.Max(0, totalMilliseconds - GetItemDuration(totalMilliseconds));
        double step = Math.Min(MaxStaggerMilliseconds, available / (count - 1));
        return Math.Min(index, count - 1) * step;
    }

    /// <summary>Startversatz, der die Mitte eines Elements auf den Ursprung (Kategorie-Element) legt.</summary>
    internal static Vector GetStartOffset(Point origin, Point itemCenter) => origin - itemCenter;

    private static bool TryGetOrigin(FrameworkElement origin, Border dock, out Point point)
    {
        point = default;
        try
        {
            point = origin.TranslatePoint(new Point(origin.ActualWidth / 2, origin.ActualHeight / 2), dock);
            return !double.IsNaN(point.X) && !double.IsNaN(point.Y);
        }
        catch (InvalidOperationException)
        {
            return false; // Kein gemeinsamer Vorfahre (z. B. Element gerade entfernt)
        }
    }

    private static void GlideItem(Button button, Border dock, Point origin, double delayMs, double durationMs)
    {
        Point center;
        try
        {
            center = button.TranslatePoint(new Point(button.ActualWidth / 2, button.ActualHeight / 2), dock);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        // Dieselbe Transform-Gruppe wie die Hover-Animationen verwenden, damit keine davon überschrieben wird.
        var group = ButtonAnimations.GetHoverTransform(button, new Point(0.5, 0.5));
        var itemScale = (ScaleTransform)group.Children[0];
        var translate = (TranslateTransform)group.Children[2];
        Vector offset = GetStartOffset(origin, center);
        var easing = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut };

        translate.BeginAnimation(TranslateTransform.XProperty, CreateAnimation(offset.X, 0, delayMs, durationMs, easing), HandoffBehavior.SnapshotAndReplace);
        translate.BeginAnimation(TranslateTransform.YProperty, CreateAnimation(offset.Y, 0, delayMs, durationMs, easing), HandoffBehavior.SnapshotAndReplace);

        var scaleAnimation = CreateAnimation(ItemStartScale, 1, delayMs, durationMs, new CubicEase { EasingMode = EasingMode.EaseOut });
        itemScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation, HandoffBehavior.SnapshotAndReplace);
        itemScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation, HandoffBehavior.SnapshotAndReplace);

        button.BeginAnimation(UIElement.OpacityProperty,
            CreateAnimation(0, 1, delayMs, durationMs * 0.5, new QuadraticEase { EasingMode = EasingMode.EaseOut }),
            HandoffBehavior.SnapshotAndReplace);
    }

    private static void StopItem(Button button)
    {
        button.BeginAnimation(UIElement.OpacityProperty, null);
        if (button.RenderTransform is TransformGroup { Children.Count: 3 } group
            && group.Children[0] is ScaleTransform itemScale
            && group.Children[2] is TranslateTransform translate
            && !group.IsFrozen)
        {
            itemScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            itemScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            translate.BeginAnimation(TranslateTransform.XProperty, null);
            translate.BeginAnimation(TranslateTransform.YProperty, null);
        }
    }

    private static void AnimateDockOpacity(Border dock, double durationMs)
    {
        dock.Opacity = 1; // Ruhewert; die Animation überdeckt ihn nur solange sie läuft
        dock.BeginAnimation(UIElement.OpacityProperty,
            CreateAnimation(0, 1, 0, durationMs, new QuadraticEase { EasingMode = EasingMode.EaseOut }),
            HandoffBehavior.SnapshotAndReplace);
    }

    /// <summary>
    /// Keyframe-Animation, die während der Verzögerung den Startwert hält (statt kurz den Ruhewert zu zeigen,
    /// wie es BeginTime täte) und danach mit Easing zum Zielwert läuft. Endet mit FillBehavior.Stop.
    /// </summary>
    private static DoubleAnimationUsingKeyFrames CreateAnimation(double from, double to, double delayMs, double durationMs, IEasingFunction easing)
    {
        var animation = new DoubleAnimationUsingKeyFrames
        {
            Duration = new Duration(TimeSpan.FromMilliseconds(delayMs + durationMs)),
            FillBehavior = FillBehavior.Stop
        };
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        if (delayMs > 0)
        {
            animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(delayMs))));
        }
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(to, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(delayMs + durationMs)), easing));
        animation.Freeze();
        return animation;
    }
}
