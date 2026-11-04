using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BiMaDock.ViewModels;

namespace BiMaDock.Views;

/// <summary>
/// Kategorie-Dock wie beim Mac: gleitet unter dem Hauptdock hervor, wächst aus der Breite des
/// Kategorie-Symbols auf volle Breite, die Symbole erscheinen nacheinander. Schließen läuft rückwärts.
/// </summary>
public partial class DockWindow
{
    private static readonly TimeSpan OpenDuration = TimeSpan.FromMilliseconds(280);
    private static readonly TimeSpan CloseDuration = TimeSpan.FromMilliseconds(170);
    private static readonly TimeSpan ItemStagger = TimeSpan.FromMilliseconds(22);
    private const double TailHalfWidth = 9;

    /// <summary>Verhindert, dass eine ältere Schließ-Animation ein neu geöffnetes Dock wieder einklappt.</summary>
    private int _flowGeneration;
    private double _collapsedScaleX = 0.2;
    private double _collapsedOffsetY = -60;

    private void OpenCategoryDock(DockItemViewModel category)
    {
        int generation = ++_flowGeneration;
        ResetCategoryTransforms();
        CategoryDock.Visibility = Visibility.Visible;
        DockArea.UpdateLayout();

        if (MainItemsControl.ItemContainerGenerator.ContainerFromItem(category) is not FrameworkElement icon)
        {
            return;
        }

        // Mittig unter das Symbol legen, ohne über den Bildschirmrand hinauszuragen.
        double iconCenter = icon.TranslatePoint(new Point(icon.ActualWidth / 2, 0), Root).X;
        double width = CategoryDock.ActualWidth;
        double naturalLeft = CategoryDock.TranslatePoint(new Point(0, 0), Root).X;
        double desiredLeft = Math.Clamp(iconCenter - (width / 2), 8, Math.Max(8, Root.ActualWidth - width - 8));
        CategoryOffset.X = desiredLeft - naturalLeft;

        double tailX = Math.Clamp(iconCenter - desiredLeft, 22, Math.Max(22, width - 22));
        CategoryTail.Margin = new Thickness(tailX - TailHalfWidth - CategoryDock.Padding.Left - 1, -16, 0, 0);

        // Startzustand: so schmal wie das Symbol und hinter dem Hauptdock versteckt.
        _collapsedScaleX = Math.Clamp(icon.ActualWidth / Math.Max(width, 1), 0.05, 1);
        _collapsedOffsetY = -(CategoryDock.ActualHeight + CategoryDock.Margin.Top);
        CategoryDock.RenderTransformOrigin = new Point(Math.Clamp(tailX / width, 0, 1), 0);

        var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };
        var spring = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.25 };
        Animate(CategoryScale, ScaleTransform.ScaleXProperty, _collapsedScaleX, 1, OpenDuration, spring);
        Animate(CategoryScale, ScaleTransform.ScaleYProperty, 0.55, 1, OpenDuration, easeOut);
        Animate(CategoryOffset, TranslateTransform.YProperty, _collapsedOffsetY, 0, OpenDuration, easeOut);
        CategoryDock.BeginAnimation(OpacityProperty, new DoubleAnimation(0.3, 1, OpenDuration * 0.6));

        StaggerItems(generation);
    }

    private void CloseCategoryDock()
    {
        if (CategoryDock.Visibility != Visibility.Visible)
        {
            return;
        }

        int generation = ++_flowGeneration;
        var easeIn = new CubicEase { EasingMode = EasingMode.EaseIn };
        Animate(CategoryScale, ScaleTransform.ScaleXProperty, null, _collapsedScaleX, CloseDuration, easeIn);
        Animate(CategoryScale, ScaleTransform.ScaleYProperty, null, 0.55, CloseDuration, easeIn);
        var slideBack = new DoubleAnimation(_collapsedOffsetY, CloseDuration) { EasingFunction = easeIn };
        slideBack.Completed += (_, _) =>
        {
            if (generation == _flowGeneration)
            {
                CategoryDock.Visibility = Visibility.Collapsed;
                ResetCategoryTransforms();
            }
        };
        CategoryOffset.BeginAnimation(TranslateTransform.YProperty, slideBack);
        CategoryDock.BeginAnimation(OpacityProperty, new DoubleAnimation(0, CloseDuration));
    }

    /// <summary>Symbole erscheinen von links nach rechts nacheinander und fallen leicht nach unten.</summary>
    private void StaggerItems(int generation)
    {
        var generator = CategoryItemsControl.ItemContainerGenerator;
        for (int i = 0; i < CategoryItemsControl.Items.Count; i++)
        {
            if (generator.ContainerFromIndex(i) is not FrameworkElement container)
            {
                continue;
            }

            var shift = new TranslateTransform(0, -10);
            container.RenderTransform = shift;
            container.Opacity = 0;
            var begin = OpenDuration * 0.3 + (ItemStagger * i);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var appear = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)) { BeginTime = begin, FillBehavior = FillBehavior.Stop };
            appear.Completed += (_, _) =>
            {
                if (generation == _flowGeneration || container.Opacity == 0)
                {
                    container.Opacity = 1;
                }
            };
            container.BeginAnimation(OpacityProperty, appear);
            shift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-10, 0, TimeSpan.FromMilliseconds(200)) { BeginTime = begin, EasingFunction = ease });
        }
    }

    private void ResetCategoryTransforms()
    {
        CategoryScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        CategoryScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        CategoryOffset.BeginAnimation(TranslateTransform.YProperty, null);
        CategoryDock.BeginAnimation(OpacityProperty, null);
        CategoryScale.ScaleX = 1;
        CategoryScale.ScaleY = 1;
        CategoryOffset.X = 0;
        CategoryOffset.Y = 0;
        CategoryDock.Opacity = 1;
    }

    private static void Animate(Animatable target, DependencyProperty property, double? from, double to, TimeSpan duration, IEasingFunction easing)
    {
        var animation = new DoubleAnimation { To = to, Duration = duration, EasingFunction = easing };
        if (from.HasValue)
        {
            animation.From = from.Value;
        }

        target.BeginAnimation(property, animation);
    }
}
