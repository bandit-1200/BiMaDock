using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BiMaDock.ViewModels;

namespace BiMaDock.Views;

/// <summary>
/// Drag &amp; Drop wie beim Mac-Dock: Beim Ziehen öffnet sich eine animierte Lücke, ein Abbild folgt der Maus,
/// über einer Kategorie wird in die Kategorie abgelegt, längeres Verweilen öffnet sie.
/// Die eigentlichen Änderungen erledigt das ViewModel.
/// </summary>
public partial class DockWindow
{
    private const string InternalFormat = "BiMaDock.EntryId";
    private static readonly Duration GapDuration = TimeSpan.FromMilliseconds(150);

    // Ziehen eines eigenen Elements
    private Point? _pressPoint;
    private DockItemViewModel? _pressItem;
    private DockItemViewModel? _draggedItem;
    private FrameworkElement? _draggedContainer;
    private DragGhost? _ghost;

    // Ziehen von außen (Explorer, Browser)
    private bool _externalDragOver;

    // Darstellung während des Ziehens
    private ItemsControl? _gapControl;
    private int _gapIndex = -1;
    private DockItemViewModel? _dropIntoCategory;
    private DispatcherTimer? _springTimer;
    private Border? _feedbackDock;

    private bool IsDragActive => _draggedItem is not null || _externalDragOver;

    // ---------- Start eines internen Ziehvorgangs ----------

    private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _pressPoint = e.GetPosition(this);
        _pressItem = (sender as FrameworkElement)?.DataContext as DockItemViewModel;
    }

    private void Item_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _pressItem is null || _pressPoint is not { } start || sender is not FrameworkElement source)
        {
            return;
        }

        var delta = e.GetPosition(this) - start;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var item = _pressItem;
        _pressItem = null;
        _pressPoint = null;
        StartInternalDrag(source, item);
    }

    private void StartInternalDrag(FrameworkElement source, DockItemViewModel item)
    {
        var owner = ItemsControl.ContainerFromElement(MainItemsControl, source) is not null ? MainItemsControl : CategoryItemsControl;
        var container = ItemsControl.ContainerFromElement(owner, source) as FrameworkElement;
        if (container is null)
        {
            return;
        }

        Mouse.Capture(null);
        _draggedItem = item;
        _draggedContainer = container;

        // Element verschwindet an seinem Platz, an dessen Stelle steht sofort eine gleich große Lücke.
        int index = VisibleContainers(owner).IndexOf(container);
        container.Visibility = Visibility.Collapsed;
        SetGap(owner, index, animate: false);

        _ghost = new DragGhost(item.Icon);
        _ghost.Show();
        _ghost.FollowCursor();
        source.GiveFeedback += Source_GiveFeedback;

        try
        {
            DragDrop.DoDragDrop(source, new DataObject(InternalFormat, item.Entry.Id.ToString()), DragDropEffects.Move);
        }
        finally
        {
            source.GiveFeedback -= Source_GiveFeedback;
            _ghost.Close();
            _ghost = null;
            EndDragVisuals();
            _draggedItem = null;
            _draggedContainer = null;
            if (!DockArea.IsMouseOver)
            {
                _hideTimer.Start();
            }
        }
    }

    private void Source_GiveFeedback(object sender, GiveFeedbackEventArgs e)
    {
        _ghost?.FollowCursor();
        e.UseDefaultCursors = true;
        e.Handled = true;
    }

    // ---------- Ziel: Haupt- und Kategorie-Dock ----------

    private void DockArea_DragEnter(object sender, DragEventArgs e)
    {
        if (_draggedItem is null)
        {
            _externalDragOver = true;
        }

        ShowDock();
        DockArea_DragOver(sender, e);
    }

    private void DockArea_DragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
        e.Effects = DragDropEffects.None;

        bool isInternal = e.Data.GetDataPresent(InternalFormat);
        if (!isInternal && ReadExternalValues(e.Data).Count == 0)
        {
            ClearDropTarget();
            return;
        }

        bool draggingCategory = _draggedItem?.IsCategory == true;
        var openCategory = _viewModel.OpenCategory;

        ItemsControl? target = null;
        if (openCategory is not null && CategoryDock.Visibility == Visibility.Visible && !draggingCategory && IsOver(CategoryDock, e))
        {
            target = CategoryItemsControl;
        }
        else if (IsOver(MainDock, e))
        {
            target = MainItemsControl;
        }

        if (target is null)
        {
            ClearDropTarget();
            return;
        }

        SetDropFeedback(target == MainItemsControl ? MainDock : CategoryDock);
        var position = e.GetPosition(target);
        if (target == MainItemsControl && !draggingCategory && CategoryUnder(position) is { } category && category != _draggedItem)
        {
            SetDropIntoCategory(category);
        }
        else
        {
            SetDropIntoCategory(null);
            SetGap(target, InsertionIndex(target, position.X), animate: true);
        }

        e.Effects = isInternal ? DragDropEffects.Move : DragDropEffects.Copy;
    }

    private void DockArea_DragLeave(object sender, DragEventArgs e)
    {
        // DragLeave kommt auch beim Wechsel zwischen Kindelementen; nur echtes Verlassen zählt.
        var p = e.GetPosition(DockArea);
        if (p.X >= 0 && p.Y >= 0 && p.X <= DockArea.ActualWidth && p.Y <= DockArea.ActualHeight)
        {
            return;
        }

        ClearDropTarget();
        if (_draggedItem is null)
        {
            _externalDragOver = false;
            _hideTimer.Start();
        }
    }

    private void DockArea_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        var into = _dropIntoCategory;
        var gapControl = _gapControl;
        int gapIndex = _gapIndex;
        Guid? dockId = gapControl == CategoryItemsControl ? _viewModel.OpenCategory?.Entry.Id : null;

        // Erst die Lücke schließen, dann die Daten ändern: das Element erscheint genau dort, wo die Lücke war.
        EndDragVisuals();
        _externalDragOver = false;

        if (into is null && gapControl is null)
        {
            return;
        }

        Guid? targetDock = into?.Entry.Id ?? dockId;
        int targetIndex = into is null ? gapIndex : int.MaxValue;

        if (e.Data.GetData(InternalFormat) is string idText && Guid.TryParse(idText, out var id))
        {
            _viewModel.MoveEntry(id, targetDock, targetIndex);
        }
        else
        {
            _viewModel.AddFromDrop(ReadExternalValues(e.Data), targetDock, targetIndex);
        }

        if (!DockArea.IsMouseOver)
        {
            _hideTimer.Start();
        }
    }

    // ---------- Lücke, Ziel-Kategorie, Federöffnen ----------

    /// <summary>Position, an der eingefügt würde: Anzahl der sichtbaren Elemente, deren Mitte links vom Mauszeiger liegt.</summary>
    private static int InsertionIndex(ItemsControl control, double x)
    {
        int index = 0;
        foreach (var container in VisibleContainers(control))
        {
            double center = container.TranslatePoint(new Point(container.ActualWidth / 2, 0), control).X;
            if (center < x)
            {
                index++;
            }
        }

        return index;
    }

    private DockItemViewModel? CategoryUnder(Point position)
    {
        foreach (var container in VisibleContainers(MainItemsControl))
        {
            if (container.DataContext is not DockItemViewModel { IsCategory: true } item)
            {
                continue;
            }

            double left = container.TranslatePoint(new Point(0, 0), MainItemsControl).X;
            double width = container.ActualWidth;
            // Nur der mittlere Teil des Symbols zählt als "hinein", die Ränder bleiben zum Einfügen daneben.
            if (position.X >= left + (width * 0.25) && position.X <= left + (width * 0.75))
            {
                return item;
            }
        }

        return null;
    }

    private void SetDropIntoCategory(DockItemViewModel? category)
    {
        if (_dropIntoCategory == category)
        {
            return;
        }

        if (_dropIntoCategory is not null)
        {
            _dropIntoCategory.IsDropTarget = false;
        }

        _dropIntoCategory = category;
        _springTimer?.Stop();
        if (category is null)
        {
            return;
        }

        // Die Lücke bleibt stehen: Schließt sie sich, verschiebt sich das Dock unter der Maus und flackert.
        category.IsDropTarget = true;

        // Kurz verweilen öffnet die Kategorie, damit man gezielt in ihr ablegen kann.
        _springTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(650) };
        _springTimer.Tick -= SpringTimer_Tick;
        _springTimer.Tick += SpringTimer_Tick;
        _springTimer.Start();
    }

    private void SpringTimer_Tick(object? sender, EventArgs e)
    {
        _springTimer?.Stop();
        if (_dropIntoCategory is { } category && _viewModel.OpenCategory != category)
        {
            _viewModel.OpenCategory = category;
        }
    }

    private void SetGap(ItemsControl? control, int index, bool animate)
    {
        if (control == _gapControl && index == _gapIndex)
        {
            return;
        }

        if (_gapControl is not null && _gapControl != control)
        {
            ApplyGap(_gapControl, -1, animate);
        }

        _gapControl = control;
        _gapIndex = control is null ? -1 : index;
        if (control is not null)
        {
            ApplyGap(control, index, animate);
        }
    }

    /// <summary>Öffnet vor dem Element an <paramref name="gap"/> eine Lücke in Elementbreite (-1 = keine Lücke).</summary>
    private void ApplyGap(ItemsControl control, int gap, bool animate)
    {
        var containers = VisibleContainers(control);
        double slot = SlotWidth(control);
        for (int i = 0; i < containers.Count; i++)
        {
            var target = new Thickness(
                i == gap ? slot : 0,
                0,
                i == containers.Count - 1 && gap == containers.Count ? slot : 0,
                0);
            SetMargin(containers[i], target, animate);
        }

        // Leeres Dock: Lücke über den Innenabstand des Steuerelements
        var padding = new Thickness(containers.Count == 0 && gap >= 0 ? slot : 0, 0, 0, 0);
        SetMargin(control, padding, animate);
    }

    private static void SetMargin(FrameworkElement element, Thickness target, bool animate)
    {
        if (!animate)
        {
            element.BeginAnimation(MarginProperty, null);
            element.Margin = target;
            return;
        }

        if (element.Margin == target && !element.HasAnimatedProperties)
        {
            return;
        }

        element.BeginAnimation(MarginProperty, new ThicknessAnimation(target, GapDuration)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        });
    }

    private double SlotWidth(ItemsControl control)
    {
        var sample = VisibleContainers(control).FirstOrDefault() ?? _draggedContainer;
        return sample is { ActualWidth: > 0 } ? sample.ActualWidth : 68;
    }

    private void ClearDropTarget()
    {
        SetDropFeedback(null);
        SetDropIntoCategory(null);
        SetGap(null, -1, animate: true);
    }

    private void EndDragVisuals()
    {
        SetDropFeedback(null);
        SetDropIntoCategory(null);
        _springTimer?.Stop();
        foreach (var control in new[] { MainItemsControl, CategoryItemsControl })
        {
            SetMargin(control, new Thickness(0), animate: false);
            foreach (var container in AllContainers(control))
            {
                SetMargin(container, new Thickness(0), animate: false);
            }
        }

        _gapControl = null;
        _gapIndex = -1;
        if (_draggedContainer is not null)
        {
            _draggedContainer.Visibility = Visibility.Visible;
        }
    }

    // ---------- Farbhinweis: Ziel-Dock färbt sich beim Ziehen ----------

    private static readonly Duration FeedbackDuration = TimeSpan.FromMilliseconds(160);

    private void SetDropFeedback(Border? dock)
    {
        if (_feedbackDock == dock)
        {
            return;
        }

        if (_feedbackDock is { } previous)
        {
            FadeBackground(previous, SurfaceColor(), clearWhenDone: true);
        }

        _feedbackDock = dock;
        if (dock is not null && Application.Current.TryFindResource("DropFeedbackColor") is Color feedback)
        {
            FadeBackground(dock, feedback, clearWhenDone: false);
        }
    }

    private void FadeBackground(Border dock, Color to, bool clearWhenDone)
    {
        var from = dock.Background is SolidColorBrush current ? current.Color : SurfaceColor();
        var brush = new SolidColorBrush(from);
        dock.Background = brush;
        var animation = new ColorAnimation(to, FeedbackDuration);
        if (clearWhenDone)
        {
            animation.Completed += (_, _) =>
            {
                // Zurück an die Farbe aus den Einstellungen binden, sofern nicht inzwischen wieder Ziel
                if (_feedbackDock != dock)
                {
                    dock.ClearValue(Border.BackgroundProperty);
                }
            };
        }

        brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
    }

    private static Color SurfaceColor() =>
        Application.Current.TryFindResource("DockSurfaceBrush") is SolidColorBrush surface ? surface.Color : Colors.Transparent;

    // ---------- Hilfsfunktionen ----------

    private static bool IsOver(FrameworkElement element, DragEventArgs e)
    {
        var p = e.GetPosition(element);
        return p.X >= 0 && p.Y >= -6 && p.X <= element.ActualWidth && p.Y <= element.ActualHeight + 6;
    }

    private static List<FrameworkElement> AllContainers(ItemsControl control) =>
        Enumerable.Range(0, control.Items.Count)
            .Select(i => control.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement)
            .OfType<FrameworkElement>()
            .ToList();

    private static List<FrameworkElement> VisibleContainers(ItemsControl control) =>
        AllContainers(control).Where(c => c.Visibility == Visibility.Visible).ToList();

    /// <summary>Dateien, Ordner und Weblinks aus einem Ziehvorgang von außen.</summary>
    private static List<string> ReadExternalValues(IDataObject data)
    {
        if (data.GetDataPresent(DataFormats.FileDrop) && data.GetData(DataFormats.FileDrop) is string[] files)
        {
            return [.. files];
        }

        foreach (var format in new[] { "UniformResourceLocatorW", DataFormats.UnicodeText, DataFormats.Text })
        {
            if (!data.GetDataPresent(format))
            {
                continue;
            }

            string? text = data.GetData(format) switch
            {
                string s => s,
                MemoryStream stream => System.Text.Encoding.Unicode.GetString(stream.ToArray()).TrimEnd('\0'),
                _ => null,
            };

            text = text?.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (Uri.TryCreate(text, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return [uri.AbsoluteUri];
            }
        }

        return [];
    }
}
