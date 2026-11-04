using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BiMaDock.Views;

/// <summary>
/// Volle Leisten: Passen nicht alle Symbole auf den Bildschirm, erscheinen links und rechts Pfeile.
/// Klick scrollt um drei Symbole, Maus auf dem Pfeil (auch beim Ziehen) scrollt fortlaufend, das Mausrad scrollt Symbol für Symbol.
/// </summary>
public partial class DockWindow
{
    /// <summary>Abstand zum Bildschirmrand, den eine Leiste mindestens frei lässt.</summary>
    private const double ScreenEdgeGap = 48;

    /// <summary>Geschwindigkeit beim Dauerscrollen in Pixel pro Sekunde.</summary>
    private const double AutoScrollSpeed = 520;

    private const double SlotPitch = ItemAnimations.SlotSize + 4;

    private DockScrollPanel? _autoScrollPanel;
    private int _autoScrollDirection;
    private TimeSpan _lastAutoScrollFrame;

    private void SetUpScrolling()
    {
        WireScroll(MainScroll, MainScrollLeft, MainScrollRight, MainDock);
        WireScroll(CategoryScroll, CategoryScrollLeft, CategoryScrollRight, CategoryDock);
        UpdateScrollLimits();
    }

    /// <summary>Breite der Leisten an den Bildschirm anpassen (Pfeile und Rand abgezogen).</summary>
    private void UpdateScrollLimits()
    {
        double limit = SystemParameters.PrimaryScreenWidth - (2 * ScreenEdgeGap) - (2 * 26) - 20;
        MainScroll.MaxViewportWidth = limit;
        CategoryScroll.MaxViewportWidth = limit;
    }

    private void WireScroll(DockScrollPanel panel, Button left, Button right, FrameworkElement surface)
    {
        void Refresh()
        {
            var visibility = panel.CanScroll ? Visibility.Visible : Visibility.Collapsed;
            left.Visibility = visibility;
            right.Visibility = visibility;
            left.IsEnabled = !panel.AtStart;
            right.IsEnabled = !panel.AtEnd;
            if (!left.IsEnabled || !right.IsEnabled)
            {
                StopAutoScroll(panel);
            }
        }

        panel.ScrollStateChanged += (_, _) => Refresh();

        left.Click += (_, _) => panel.ScrollBy(-3 * SlotPitch);
        right.Click += (_, _) => panel.ScrollBy(3 * SlotPitch);

        // Maus auf dem Pfeil halten scrollt fortlaufend (kurze Verzögerung, damit ein Klick nicht doppelt wirkt)
        left.MouseEnter += (_, _) => StartAutoScroll(panel, -1);
        right.MouseEnter += (_, _) => StartAutoScroll(panel, 1);
        left.MouseLeave += (_, _) => StopAutoScroll(panel);
        right.MouseLeave += (_, _) => StopAutoScroll(panel);

        // Beim Ziehen: Ablegen auf dem Pfeil gibt es nicht, aber Darüberhalten scrollt.
        left.DragEnter += (_, e) => { StartAutoScroll(panel, -1); e.Handled = true; };
        right.DragEnter += (_, e) => { StartAutoScroll(panel, 1); e.Handled = true; };
        left.DragLeave += (_, _) => StopAutoScroll(panel);
        right.DragLeave += (_, _) => StopAutoScroll(panel);
        left.Drop += (_, _) => StopAutoScroll(panel);
        right.Drop += (_, _) => StopAutoScroll(panel);

        surface.PreviewMouseWheel += (_, e) =>
        {
            if (panel.CanScroll)
            {
                panel.ScrollBy(-Math.Sign(e.Delta) * SlotPitch, 160);
                e.Handled = true;
            }
        };
    }

    private void StartAutoScroll(DockScrollPanel panel, int direction)
    {
        _autoScrollPanel = panel;
        _autoScrollDirection = direction;
        _lastAutoScrollFrame = TimeSpan.Zero;
        CompositionTarget.Rendering -= AutoScrollFrame;
        CompositionTarget.Rendering += AutoScrollFrame;
    }

    private void StopAutoScroll(DockScrollPanel panel)
    {
        if (_autoScrollPanel == panel)
        {
            _autoScrollPanel = null;
            CompositionTarget.Rendering -= AutoScrollFrame;
        }
    }

    private void AutoScrollFrame(object? sender, EventArgs e)
    {
        var now = ((RenderingEventArgs)e).RenderingTime;
        if (_autoScrollPanel is null || now == _lastAutoScrollFrame)
        {
            return;
        }

        // Das erste Bild nur merken: so beginnt das Scrollen erst nach kurzem Verweilen nicht ruckartig.
        if (_lastAutoScrollFrame == TimeSpan.Zero)
        {
            _lastAutoScrollFrame = now;
            return;
        }

        double dt = Math.Min((now - _lastAutoScrollFrame).TotalSeconds, 0.05);
        _lastAutoScrollFrame = now;
        _autoScrollPanel.Nudge(_autoScrollDirection * AutoScrollSpeed * dt);
    }

    /// <summary>Scrollt die Leiste so, dass das Symbol eines Eintrags sichtbar ist (z. B. nach dem Hinzufügen).</summary>
    private static void ScrollIntoView(DockScrollPanel panel, ItemsControl items, object item)
    {
        if (panel.CanScroll && items.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement container)
        {
            double left = container.TranslatePoint(new Point(0, 0), items).X;
            panel.BringIntoView(left, left + container.ActualWidth);
        }
    }
}
