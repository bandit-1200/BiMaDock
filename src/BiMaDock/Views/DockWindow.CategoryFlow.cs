using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BiMaDock.ViewModels;

namespace BiMaDock.Views;

/// <summary>
/// Kategorie-Dock fließt wie Wasser aus dem Hauptdock: Strahl unter dem Symbol, Tropfen, der sich zur
/// Dock-Fläche ausbreitet (<see cref="LiquidFlow"/>). Erst dann erscheinen Rahmen und Symbole.
/// Schließen läuft rückwärts zurück ins Hauptdock.
/// </summary>
public partial class DockWindow
{
    private TimeSpan OpenDuration => TimeSpan.FromMilliseconds(_viewModel.Settings.FlowDurationMs);

    private TimeSpan CloseDuration => OpenDuration * 0.58;
    private static readonly TimeSpan ItemStagger = TimeSpan.FromMilliseconds(24);

    /// <summary>Verhindert, dass eine ältere Animation ein inzwischen anders geöffnetes Dock verändert.</summary>
    private int _flowGeneration;

    private void OpenCategoryDock(DockItemViewModel category)
    {
        int generation = ++_flowGeneration;
        StopFlowAnimations();
        CategoryOffset.X = 0;
        CategoryDock.Opacity = 0;
        CategoryDock.Visibility = Visibility.Visible;
        DockArea.UpdateLayout();

        if (MainItemsControl.ItemContainerGenerator.ContainerFromItem(category) is not FrameworkElement icon)
        {
            CategoryDock.Opacity = 1;
            return;
        }

        // Mittig unter das Symbol legen, ohne über den Bildschirmrand hinauszuragen.
        double iconCenter = icon.TranslatePoint(new Point(icon.ActualWidth / 2, 0), Root).X;
        double width = CategoryDock.ActualWidth;
        double naturalLeft = CategoryDock.TranslatePoint(new Point(0, 0), Root).X;
        double desiredLeft = Math.Clamp(iconCenter - (width / 2), 8, Math.Max(8, Root.ActualWidth - width - 8));
        CategoryOffset.X = desiredLeft - naturalLeft;

        // Austrittspunkt: Unterkante des Hauptdocks unter dem Symbol; Ziel: endgültige Fläche des Kategorie-Docks.
        var source = new Point(
            icon.TranslatePoint(new Point(icon.ActualWidth / 2, 0), Liquid).X,
            MainDock.TranslatePoint(new Point(0, MainDock.ActualHeight), Liquid).Y);
        var targetTopLeft = CategoryDock.TranslatePoint(new Point(0, 0), Liquid);
        Liquid.Configure(source, icon.ActualWidth, new Rect(targetTopLeft, CategoryDock.RenderSize), CategoryDock.CornerRadius.TopLeft);

        if (!_viewModel.Settings.FlowAnimationEnabled)
        {
            // Ohne Animation: fertige Form sofort zeigen (Steg zum Symbol bleibt)
            Liquid.Progress = 1;
            CategoryDock.Opacity = 1;
            return;
        }

        HideCategoryItems();
        var flow = new DoubleAnimation(0, 1, OpenDuration);
        Liquid.BeginAnimation(LiquidFlow.ProgressProperty, flow);

        // Rahmen und Symbole erst, wenn die Flüssigkeit fast ihre Form hat.
        var frameIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140)) { BeginTime = OpenDuration * 0.82 };
        CategoryDock.BeginAnimation(OpacityProperty, frameIn);
        RevealItems(generation, OpenDuration * 0.78);
    }

    private void CloseCategoryDock()
    {
        if (CategoryDock.Visibility != Visibility.Visible)
        {
            return;
        }

        int generation = ++_flowGeneration;
        if (!_viewModel.Settings.FlowAnimationEnabled)
        {
            CategoryDock.Visibility = Visibility.Collapsed;
            StopFlowAnimations();
            return;
        }

        CategoryDock.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(90)));

        var drain = new DoubleAnimation(0, CloseDuration) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseIn } };
        drain.Completed += (_, _) =>
        {
            if (generation == _flowGeneration)
            {
                CategoryDock.Visibility = Visibility.Collapsed;
                StopFlowAnimations();
            }
        };
        Liquid.BeginAnimation(LiquidFlow.ProgressProperty, drain);
    }

    private void HideCategoryItems()
    {
        foreach (var container in AllContainers(CategoryItemsControl))
        {
            container.BeginAnimation(OpacityProperty, null);
            container.Opacity = 0;
        }
    }

    /// <summary>Symbole tauchen von links nach rechts nacheinander auf und sinken leicht ein, wie auf Wasser.</summary>
    private void RevealItems(int generation, TimeSpan start)
    {
        var containers = AllContainers(CategoryItemsControl);
        for (int i = 0; i < containers.Count; i++)
        {
            var container = containers[i];
            var shift = new TranslateTransform(0, -8);
            container.RenderTransform = shift;
            var begin = start + (ItemStagger * i);

            var appear = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)) { BeginTime = begin, FillBehavior = FillBehavior.Stop };
            appear.Completed += (_, _) =>
            {
                if (generation == _flowGeneration)
                {
                    container.Opacity = 1;
                }
            };
            container.BeginAnimation(OpacityProperty, appear);
            shift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-8, 0, TimeSpan.FromMilliseconds(260))
            {
                BeginTime = begin,
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.6 },
            });
        }
    }

    private void StopFlowAnimations()
    {
        Liquid.BeginAnimation(LiquidFlow.ProgressProperty, null);
        Liquid.Progress = 0;
        CategoryDock.BeginAnimation(OpacityProperty, null);
        CategoryDock.Opacity = 1;
    }
}
