using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BiMaDock.Native;
using BiMaDock.ViewModels;
using Microsoft.Extensions.Logging;

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
    private DockItemViewModel? _openWithTarget;
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

        ResetMagnifyNow();

        // Element verschwindet an seinem Platz, an dessen Stelle steht sofort eine gleich große Lücke.
        int index = VisibleContainers(owner).IndexOf(container);
        container.Visibility = Visibility.Collapsed;
        SetGap(owner, index, animate: false);

        _ghost = new DragGhost(item.IsSeparator ? SeparatorGhostImage() : item.Icon);
        _ghost.Show();
        _ghost.FollowCursor();
        source.GiveFeedback += Source_GiveFeedback;

        bool removeSeparator = false;
        try
        {
            var effect = DragDrop.DoDragDrop(source, new DataObject(InternalFormat, item.Entry.Id.ToString()), DragDropEffects.Move);

            // Trenner aus dem Dock gezogen und losgelassen: entfernen (Rückgängig stellt ihn wieder her)
            removeSeparator = item.IsSeparator && effect == DragDropEffects.None && !IsCursorOverDock();
        }
        finally
        {
            source.GiveFeedback -= Source_GiveFeedback;
            _ghost.Close();
            _ghost = null;
            EndDragVisuals();
            _draggedItem = null;
            _draggedContainer = null;
            if (removeSeparator)
            {
                _viewModel.RemoveEntry(item);
            }

            if (!DockArea.IsMouseOver)
            {
                _hideTimer.Start();
            }
        }
    }

    private bool IsCursorOverDock()
    {
        if (!BiMaDock.Native.WindowStyles.TryGetCursorPos(out int x, out int y))
        {
            return true; // im Zweifel nichts löschen
        }

        var p = DockArea.PointFromScreen(new Point(x, y));
        return p.X >= 0 && p.Y >= 0 && p.X <= DockArea.ActualWidth && p.Y <= DockArea.ActualHeight;
    }

    /// <summary>Abbild beim Ziehen eines Trenners: heller senkrechter Strich in der Größe eines Symbols.</summary>
    private static ImageSource SeparatorGhostImage()
    {
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 52, 52))));
        group.Children.Add(new GeometryDrawing(Brushes.White, null, new RectangleGeometry(new Rect(24, 6, 4, 40), 2, 2)));
        var image = new DrawingImage(group);
        image.Freeze();
        return image;
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
        try
        {
            if (_draggedItem is null)
            {
                _externalDragOver = true;
                DragLog.LogInformation("DragEnter von außen. Formate: {Formats}", FormatList(e.Data));
            }

            PrepareDragData(e.Data);
            RestartDragWatchdog();
            ShowDock();
            HandleDragOver(e);
        }
        catch (Exception ex)
        {
            FailDrag(e, ex, "DragEnter");
        }
    }

    private void DockArea_DragOver(object sender, DragEventArgs e)
    {
        try
        {
            PrepareDragData(e.Data);
            RestartDragWatchdog();
            HandleDragOver(e);
        }
        catch (Exception ex)
        {
            FailDrag(e, ex, "DragOver");
        }
    }

    /// <summary>
    /// Wirkung für einen Ziehvorgang von außen. Die Quelle gibt vor, was erlaubt ist: Der Explorer erlaubt Copy,
    /// das Windows-Startmenü nur Link (Verknüpfung). Eine nicht erlaubte Wirkung würde das Ablegen stillschweigend verwerfen.
    /// </summary>
    private static DragDropEffects ExternalEffect(DragEventArgs e)
    {
        var allowed = e.AllowedEffects;
        if (allowed.HasFlag(DragDropEffects.Copy))
        {
            return DragDropEffects.Copy;
        }

        if (allowed.HasFlag(DragDropEffects.Link))
        {
            return DragDropEffects.Link;
        }

        return allowed.HasFlag(DragDropEffects.Move) ? DragDropEffects.Move : DragDropEffects.None;
    }

    private string? _lastDragState;

    /// <summary>Schreibt den Zustand eines Ziehvorgangs nur bei Änderung ins Protokoll (Diagnose).</summary>
    private void LogDragState(string state)
    {
        if (_lastDragState != state)
        {
            _lastDragState = state;
            DragLog.LogInformation("Ziehen: {State}", state);
        }
    }

    private void HandleDragOver(DragEventArgs e)
    {
        e.Handled = true;
        e.Effects = DragDropEffects.None;
        HandleDragOverCore(e);
        if (_draggedItem is null)
        {
            var pos = e.GetPosition(DockArea);
            LogDragState($"Effekt {e.Effects} (erlaubt {e.AllowedEffects}), Position {pos.X:0},{pos.Y:0}, Dock {DockArea.ActualWidth:0}x{DockArea.ActualHeight:0}, MainDock {MainDock.ActualWidth:0}x{MainDock.ActualHeight:0}, gezeigt {_isShown}");
        }
    }

    private void HandleDragOverCore(DragEventArgs e)
    {

        var data = _dragData;
        bool isInternal = data?.IsInternal == true;
        if (data is null || (!isInternal && data.Values.Count == 0))
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

        // Dateien von außen auf ein Programm gezogen: damit öffnen statt als Eintrag hinzufügen
        if (!isInternal && data.Files.Count > 0
            && ItemUnder(target, position, i => i.AcceptsFiles) is { } program)
        {
            SetDropIntoCategory(null);
            SetGap(null, -1, animate: true);
            SetOpenWith(program);
            e.Effects = ExternalEffect(e);
            return;
        }

        SetOpenWith(null);
        if (target == MainItemsControl && !draggingCategory && CategoryUnder(position) is { } category && category != _draggedItem)
        {
            SetDropIntoCategory(category);
        }
        else
        {
            SetDropIntoCategory(null);
            SetGap(target, InsertionIndex(target, position.X), animate: true);
        }

        e.Effects = isInternal ? DragDropEffects.Move : ExternalEffect(e);
    }

    private void DockArea_DragLeave(object sender, DragEventArgs e)
    {
        try
        {
            if (_draggedItem is null)
            {
                DragLog.LogInformation("DragLeave (Zeiger im Dock: {Over})", IsCursorOverDockStrict());
                _lastDragState = null;
            }

            // DragLeave kommt auch beim Wechsel zwischen Kindelementen. Nur wenn der Zeiger sicher noch im Dock ist,
            // wird nicht aufgeräumt (der Sicherheits-Timer fängt den Rest ab). Im Zweifel wird aufgeräumt.
            if (IsCursorOverDockStrict() && IsInsideDockArea(e))
            {
                return;
            }

            if (_draggedItem is null)
            {
                CancelExternalDrag();
            }
            else
            {
                ClearDropTarget();
            }
        }
        catch (Exception ex)
        {
            DragLog.LogError(ex, "Fehler beim Verlassen des Docks beim Ziehen");
            CancelExternalDrag();
        }
    }

    private bool IsInsideDockArea(DragEventArgs e)
    {
        var p = e.GetPosition(DockArea);
        return p.X >= 0 && p.Y >= 0 && p.X <= DockArea.ActualWidth && p.Y <= DockArea.ActualHeight;
    }

    /// <summary>Wie <see cref="IsCursorOverDock"/>, aber im Zweifel "nein" (dann wird aufgeräumt).</summary>
    private bool IsCursorOverDockStrict()
    {
        if (!BiMaDock.Native.WindowStyles.TryGetCursorPos(out int x, out int y))
        {
            return false;
        }

        var p = DockArea.PointFromScreen(new Point(x, y));
        return p.X >= 0 && p.Y >= 0 && p.X <= DockArea.ActualWidth && p.Y <= DockArea.ActualHeight;
    }

    private void DockArea_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        _lastDragState = null;
        if (_draggedItem is null)
        {
            DragLog.LogInformation("Drop von außen");
        }

        try
        {
            var openWith = _openWithTarget;
            var into = _dropIntoCategory;
            var gapControl = _gapControl;
            int gapIndex = _gapIndex;
            Guid? dockId = gapControl == CategoryItemsControl ? _viewModel.OpenCategory?.Entry.Id : null;
            var data = _dragData ?? ReadDragData(e.Data);

            // Erst die Lücke schließen, dann die Daten ändern: das Element erscheint genau dort, wo die Lücke war.
            EndDragVisuals();
            StopDragWatchdog();
            _externalDragOver = false;
            _dragData = null;

            if (openWith is not null)
            {
                _viewModel.OpenFilesWith(openWith, data.Files);
                return;
            }

            if (into is null && gapControl is null)
            {
                return;
            }

            Guid? targetDock = into?.Entry.Id ?? dockId;
            int targetIndex = into is null ? gapIndex : int.MaxValue;

            if (data.IsInternal && SafeGetData(e.Data, InternalFormat) is string idText && Guid.TryParse(idText, out var id))
            {
                _viewModel.MoveEntry(id, targetDock, targetIndex);
            }
            else if (data.Values.Count > 0)
            {
                _viewModel.AddFromDrop(data.Values, targetDock, targetIndex, data.Names);
            }
            else
            {
                DragLog.LogWarning("Drop ohne verwertbare Daten ignoriert. Formate: {Formats}", FormatList(e.Data));
            }

            if (!DockArea.IsMouseOver)
            {
                _hideTimer.Start();
            }
        }
        catch (Exception ex)
        {
            FailDrag(e, ex, "Drop");
        }
    }

    // ---------- Absicherung: Daten einmal lesen, Aufräumen, Sicherheits-Timer ----------

    /// <summary>Gelesene Daten eines Ziehvorgangs, damit sie nicht bei jedem DragOver neu abgefragt werden.</summary>
    private sealed record DragData(IDataObject Source, bool IsInternal, List<string> Files, List<string> Values, Dictionary<string, string> Names);

    private DragData? _dragData;
    private DispatcherTimer? _dragWatchdog;

    private ILogger DragLog => FullscreenLog;

    private void PrepareDragData(IDataObject source)
    {
        if (_dragData is not null && ReferenceEquals(_dragData.Source, source))
        {
            return;
        }

        _dragData = ReadDragData(source);
        if (!_dragData.IsInternal && _dragData.Values.Count == 0)
        {
            // Unbekanntes Format (z. B. aus dem Startmenü): einmal pro Ziehvorgang festhalten, was angeboten wird
            DragLog.LogInformation("Ziehen von außen ohne verwertbare Daten. Angebotene Formate: {Formats}", FormatList(source));
        }
    }

    private DragData ReadDragData(IDataObject source)
    {
        bool isInternal = SafeGetDataPresent(source, InternalFormat);
        List<string> files = isInternal ? [] : ReadDroppedFiles(source);
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        List<string> values = isInternal ? [] : ReadExternalValues(source, names);
        return new DragData(source, isInternal, files, values, names);
    }

    private static string FormatList(IDataObject data)
    {
        try
        {
            return string.Join(", ", data.GetFormats());
        }
        catch (Exception ex) when (IsDataAccessError(ex))
        {
            return "(nicht lesbar: " + ex.GetType().Name + ")";
        }
    }

    private void FailDrag(DragEventArgs e, Exception ex, string phase)
    {
        e.Handled = true;
        e.Effects = DragDropEffects.None;
        DragLog.LogError(ex, "Fehler in {Phase} beim Ziehen. Formate: {Formats}", phase, FormatList(e.Data));
        try
        {
            if (_draggedItem is null)
            {
                CancelExternalDrag();
            }
            else
            {
                ClearDropTarget();
            }
        }
        catch (Exception cleanup)
        {
            DragLog.LogError(cleanup, "Aufräumen nach Fehler in {Phase} schlug fehl", phase);
        }
    }

    /// <summary>Räumt alles auf, was ein Ziehvorgang von außen hinterlässt (Lücke, Farbe, Zustand).</summary>
    private void CancelExternalDrag()
    {
        StopDragWatchdog();
        _externalDragOver = false;
        _dragData = null;
        ClearDropTarget();
        EndDragVisuals();
        if (!DockArea.IsMouseOver)
        {
            _hideTimer.Start();
        }
    }

    private void RestartDragWatchdog()
    {
        if (_dragWatchdog is null)
        {
            _dragWatchdog = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _dragWatchdog.Tick += DragWatchdog_Tick;
        }

        _dragWatchdog.Stop();
        _dragWatchdog.Start();
    }

    private void StopDragWatchdog() => _dragWatchdog?.Stop();

    private void DragWatchdog_Tick(object? sender, EventArgs e)
    {
        // Seit 300 ms kein DragOver mehr. Das Startmenü (und andere Quellen) meldet DragOver nur bei Mausbewegung,
        // ein ruhig gehaltener Zeiger über dem Dock ist also kein Abbruch: solange die Taste gedrückt ist und
        // der Zeiger im Dock steht, weiter warten.
        _dragWatchdog?.Stop();
        try
        {
            if (_draggedItem is null && BiMaDock.Native.WindowStyles.IsLeftButtonDown() && IsCursorOverDockStrict())
            {
                _dragWatchdog?.Start();
                return;
            }

            if (_draggedItem is null)
            {
                CancelExternalDrag();
            }
            else
            {
                ClearDropTarget();
            }
        }
        catch (Exception ex)
        {
            DragLog.LogError(ex, "Fehler im Sicherheits-Timer beim Ziehen");
        }
    }

    private static bool SafeGetDataPresent(IDataObject data, string format)
    {
        try
        {
            return data.GetDataPresent(format);
        }
        catch (Exception ex) when (IsDataAccessError(ex))
        {
            return false;
        }
    }

    private static object? SafeGetData(IDataObject data, string format)
    {
        try
        {
            return data.GetDataPresent(format) ? data.GetData(format) : null;
        }
        catch (Exception ex) when (IsDataAccessError(ex))
        {
            return null;
        }
    }

    private static bool IsDataAccessError(Exception ex) =>
        ex is COMException or InvalidCastException or ArgumentException or NotSupportedException
            or InvalidOperationException or IOException or OutOfMemoryException or UnauthorizedAccessException;

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

    /// <summary>Element unter der Maus (mittlerer Teil), das <paramref name="accepts"/> erfüllt.</summary>
    private static DockItemViewModel? ItemUnder(ItemsControl control, Point position, Func<DockItemViewModel, bool> accepts)
    {
        foreach (var container in VisibleContainers(control))
        {
            if (container.DataContext is not DockItemViewModel item || !accepts(item))
            {
                continue;
            }

            double left = container.TranslatePoint(new Point(0, 0), control).X;
            double width = container.ActualWidth;
            if (position.X >= left + (width * 0.25) && position.X <= left + (width * 0.75))
            {
                return item;
            }
        }

        return null;
    }

    private void SetOpenWith(DockItemViewModel? item)
    {
        if (_openWithTarget == item)
        {
            return;
        }

        if (_openWithTarget is not null)
        {
            _openWithTarget.IsDropTarget = false;
        }

        _openWithTarget = item;
        if (item is not null)
        {
            item.IsDropTarget = true;
        }
    }

    /// <summary>Echte Dateien aus dem Explorer (keine Weblinks).</summary>
    private static List<string> ReadDroppedFiles(IDataObject data) =>
        SafeGetData(data, DataFormats.FileDrop) is string[] files ? [.. files.Where(f => !string.IsNullOrWhiteSpace(f) && (File.Exists(f) || Directory.Exists(f)))] : [];

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
        // Lücke so breit wie das gezogene Element, sonst wie ein Symbol (nicht wie ein schmaler Trenner)
        var sample = _draggedContainer
            ?? VisibleContainers(control).FirstOrDefault(c => c.DataContext is DockItemViewModel { IsSeparator: false });
        return sample is { ActualWidth: > 0 } ? sample.ActualWidth : 68;
    }

    private void ClearDropTarget()
    {
        SetOpenWith(null);
        SetDropFeedback(null);
        SetDropIntoCategory(null);
        SetGap(null, -1, animate: true);
    }

    private void EndDragVisuals()
    {
        SetOpenWith(null);
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
        // Die sichtbare Fläche liegt als eigenes Element hinter dem Dock (Schatten ohne Inhalt).
        var surface = dock == MainDock ? MainSurface : CategorySurface;
        var from = surface.Background is SolidColorBrush current ? current.Color : SurfaceColor();
        var brush = new SolidColorBrush(from);
        surface.Background = brush;
        var animation = new ColorAnimation(to, FeedbackDuration);
        if (clearWhenDone)
        {
            animation.Completed += (_, _) =>
            {
                // Zurück an die Farbe aus den Einstellungen binden, sofern nicht inzwischen wieder Ziel
                if (_feedbackDock != dock)
                {
                    surface.ClearValue(Border.BackgroundProperty);
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
    private List<string> ReadExternalValues(IDataObject data, Dictionary<string, string> names)
    {
        // Echte Dateien und Ordner (Explorer). Das Startmenü liefert unter FileDrop bei Store-Apps nur eine
        // AppID ohne Pfad, dann gilt die IDList unten.
        if (SafeGetData(data, DataFormats.FileDrop) is string[] files
            && files.Length > 0
            && files.All(f => File.Exists(f) || Directory.Exists(f)))
        {
            return [.. files];
        }

        // Startmenü und Shell-Ordner: Elemente als IDList (Programme, Store-Apps)
        if (SafeGetData(data, ShellDrop.Format) is MemoryStream idList)
        {
            var shellValues = new List<string>();
            try
            {
                foreach (var item in ShellDrop.Read(idList.ToArray()))
                {
                    shellValues.Add(item.Target);
                    names[item.Target] = item.Name;
                }
            }
            catch (Exception ex) when (IsDataAccessError(ex))
            {
                DragLog.LogWarning(ex, "Shell-Elemente konnten nicht gelesen werden");
            }

            if (shellValues.Count > 0)
            {
                return shellValues;
            }
        }

        foreach (var format in new[] { "UniformResourceLocatorW", DataFormats.UnicodeText, DataFormats.Text })
        {
            string? text = SafeGetData(data, format) switch
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

        return SafeGetData(data, DataFormats.FileDrop) is string[] rest ? [.. rest.Where(f => !string.IsNullOrWhiteSpace(f))] : [];
    }
}
