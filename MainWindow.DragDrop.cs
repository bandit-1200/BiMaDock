using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BiMaDock
{
    public partial class MainWindow
    {
        public void SetDragging(bool value)
        {
            isDragging = value;
        }

        public Point? ActiveDragStartPoint
        {
            get => activeDragStartPoint;
            set => activeDragStartPoint = value;
        }

        public Button? ActiveDraggedButton
        {
            get => activeDraggedButton;
            set => activeDraggedButton = value;
        }

        private Point? activeDragStartPoint;
        private Button? activeDraggedButton;

        private void MainWindow_DragEnter(object sender, DragEventArgs e)
        {
            ShowDockForDrag(e);
        }

        private void MainWindow_DragOver(object sender, DragEventArgs e)
        {
            ShowDockForDrag(e);
        }

        private void MainDockBorder_DragEnter(object sender, DragEventArgs e)
        {
            ShowDockForDrag(e);
        }

        private void MainDockBorder_DragOver(object sender, DragEventArgs e)
        {
            ShowDockForDrag(e);
        }

        // Fenster und Rahmen blenden das Dock nur ein; ein Ablegen ist dort nicht möglich.
        // Echte Ziele (DockPanel, Kategorie-Dock) setzen e.Handled, daher kommt das Ereignis hier nicht an.
        private void ShowDockForDrag(DragEventArgs e)
        {
            if (GetDropEffect(e.Data) != DragDropEffects.None)
            {
                ShowDock();
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        internal static DragDropEffects GetDropEffect(IDataObject data)
        {
            if (data.GetDataPresent(DockDropPosition.DockItemButtonFormat))
            {
                return DragDropEffects.Move;
            }

            return DockDropPosition.IsSupportedDrop(data)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private const double DragAutoScrollMargin = 30.0;
        private const double DragAutoScrollStep = 12.0;

        // Scrollt ein überlaufendes Dock, wenn der Zeiger beim Ziehen nahe am linken oder rechten Rand ist.
        private static void AutoScrollDuringDrag(ScrollViewer scrollViewer, DragEventArgs e)
        {
            if (scrollViewer.ScrollableWidth <= 0)
            {
                return;
            }

            double x = e.GetPosition(scrollViewer).X;
            if (x < DragAutoScrollMargin && scrollViewer.HorizontalOffset > 0)
            {
                scrollViewer.ScrollToHorizontalOffset(Math.Max(0, scrollViewer.HorizontalOffset - DragAutoScrollStep));
            }
            else if (x > scrollViewer.ActualWidth - DragAutoScrollMargin &&
                     scrollViewer.HorizontalOffset < scrollViewer.ScrollableWidth)
            {
                scrollViewer.ScrollToHorizontalOffset(Math.Min(scrollViewer.ScrollableWidth, scrollViewer.HorizontalOffset + DragAutoScrollStep));
            }
        }

        // Wird nach DoDragDrop (auch bei Abbruch) und zu Beginn von DockManager.DockPanel_Drop aufgerufen
        // und räumt alle Drag-Rückmeldungen auf.
        public void CleanupAfterDrag()
        {
            // Lücken und ausgeblendeten Quell-Button erst nach dem laufenden Ereignis zurücksetzen:
            // DockManager berechnet die Einfügeposition erst nach diesem Aufruf und soll dabei dasselbe
            // Layout sehen wie das letzte DragOver. Die Priorität liegt über Render, daher ist vor dem
            // nächsten Bild alles entfernt (sichtbar wie ein sofortiges Entfernen).
            Dispatcher.InvokeAsync(ResetDropGaps, DispatcherPriority.Normal);
            dragLeaveCheckTimer?.Stop();
            lastDragCategoryId = null;

            var primaryBrush = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
            DockPanel.Background = primaryBrush;
            CategoryDockContainer.Background = primaryBrush;

            currentDockStatus &= ~DockStatus.DraggingToDock;
            CheckAllConditions();
        }

        private string? lastDragCategoryId = null; // Zuletzt beim Ziehen geöffnete Kategorie

        #region Animierte Einfügelücke

        // Breite eines Dock-Buttons inklusive Außenabstand (70 + 2 * 5), falls nichts gemessen werden kann.
        private const double DefaultDropGapWidth = 80.0;
        private const int DragLeaveConfirmMilliseconds = 400;

        private DockDropGap? mainDockGap;
        private DockDropGap? categoryDockGap;
        private Button? hiddenDragSource; // Beim internen Ziehen ausgeblendeter Quell-Button
        private double dragSourceSlotWidth = DefaultDropGapWidth;
        private long lastDragActivityTick;
        private DispatcherTimer? dragLeaveCheckTimer;

        private DockDropGap MainDockGap => mainDockGap ??= new DockDropGap(DockPanel);
        private DockDropGap CategoryDockGap => categoryDockGap ??= new DockDropGap(CategoryDockContainer);

        private static double GetSlotWidth(FrameworkElement element)
        {
            return element.ActualWidth + element.Margin.Left + element.Margin.Right;
        }

        // Breite der Lücke: Platz des gezogenen Buttons, sonst eines vorhandenen Buttons, sonst Standard.
        private double GetDropGapWidth(Panel panel)
        {
            if (hiddenDragSource != null)
            {
                return dragSourceSlotWidth;
            }

            var sample = panel.Children.OfType<Button>()
                .FirstOrDefault(button => button.Visibility == Visibility.Visible && button.ActualWidth > 0);
            return sample != null ? GetSlotWidth(sample) : DefaultDropGapWidth;
        }

        // Blendet beim internen Ziehen den Quell-Button aus. An seiner Stelle steht zunächst eine gleich
        // breite Lücke: Im Ziel-Dock bleibt sie offen (nichts springt), in einem anderen Dock schließt sie sich.
        private void HideDragSource(IDataObject data, Panel targetPanel)
        {
            if (!data.GetDataPresent(DockDropPosition.DockItemButtonFormat) ||
                data.GetData(DockDropPosition.DockItemButtonFormat) is not Button source ||
                ReferenceEquals(source, hiddenDragSource) ||
                source.Visibility != Visibility.Visible)
            {
                return;
            }

            RestoreDragSource();
            hiddenDragSource = source;
            dragSourceSlotWidth = source.ActualWidth > 0 ? GetSlotWidth(source) : DefaultDropGapWidth;

            var owner = VisualTreeHelper.GetParent(source) as Panel;
            DockDropGap? ownerGap = ReferenceEquals(owner, DockPanel) ? MainDockGap
                : ReferenceEquals(owner, CategoryDockContainer) ? CategoryDockGap
                : null;
            if (ownerGap == null)
            {
                source.Visibility = Visibility.Collapsed;
                return;
            }

            ownerGap.ReplaceButton(source, dragSourceSlotWidth, keepOpen: ReferenceEquals(owner, targetPanel));
        }

        private void RestoreDragSource()
        {
            if (hiddenDragSource != null)
            {
                hiddenDragSource.Visibility = Visibility.Visible;
                hiddenDragSource = null;
            }
        }

        // Entfernt alle Lücken sofort und blendet den Quell-Button (an seiner aktuellen Position) wieder ein.
        private void ResetDropGaps()
        {
            mainDockGap?.RemoveAll();
            categoryDockGap?.RemoveAll();
            RestoreDragSource();
        }

        // Öffnet die Lücke an der Einfügeposition; bleibt diese gleich, passiert nichts.
        private void UpdateDropGap(Panel panel, DockDropGap gap, IDataObject data, double dropX)
        {
            HideDragSource(data, panel);
            int itemIndex = DockDropPosition.FindItemInsertionIndex(panel, dropX);
            gap.OpenAt(itemIndex, GetDropGapWidth(panel));
        }

        private void NoteDragActivity()
        {
            lastDragActivityTick = Environment.TickCount64;
        }

        // Ein DragLeave innerhalb der Grenzen ist meist nur ein Wechsel auf ein Kindelement, kann aber
        // auch ein Abbruch (Esc) eines externen Drags sein. Folgt kein DragOver mehr, wird die Lücke geschlossen.
        private void ConfirmDragLeaveLater()
        {
            if (dragLeaveCheckTimer == null)
            {
                dragLeaveCheckTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(DragLeaveConfirmMilliseconds)
                };
                dragLeaveCheckTimer.Tick += DragLeaveCheckTimer_Tick;
            }

            dragLeaveCheckTimer.Stop();
            dragLeaveCheckTimer.Start();
        }

        private void DragLeaveCheckTimer_Tick(object? sender, EventArgs e)
        {
            dragLeaveCheckTimer?.Stop();

            // Interne Drags räumt CleanupAfterDrag zuverlässig auf.
            if (isDragging || Environment.TickCount64 - lastDragActivityTick < DragLeaveConfirmMilliseconds)
            {
                return;
            }

            mainDockGap?.Close(animate: true);
            categoryDockGap?.Close(animate: true);

            // Abgebrochener externer Drag (z. B. Esc): auch Hervorhebung und Drag-Status zurücksetzen,
            // sonst bleibt das Dock eingeblendet.
            lastDragCategoryId = null;
            var primaryBrush = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
            DockPanel.Background = primaryBrush;
            CategoryDockContainer.Background = primaryBrush;
            currentDockStatus &= ~DockStatus.DraggingToDock;
            CheckAllConditions();
        }

        // Verwaltet die Lücke eines Docks, die beim Ziehen die Einfügeposition freihält.
        // Die Breite der Lücke wird animiert; dadurch gleiten die Nachbarn im Layout zur Seite.
        // Beim Wechsel der Position schließt sich die alte Lücke, während sich die neue öffnet
        // (gleiche Dauer und Kurve, daher bleibt die Gesamtbreite konstant).
        private sealed class DockDropGap
        {
            private static readonly Duration AnimationDuration = new(TimeSpan.FromMilliseconds(150));
            private static readonly IEasingFunction Easing = CreateEasing();

            private readonly Panel panel;
            private readonly Dictionary<Border, DoubleAnimation> closingGaps = new();
            private Border? openGap;
            private int openIndex = -1; // Position unter den sichtbaren Buttons

            public DockDropGap(Panel panel)
            {
                this.panel = panel;
            }

            private static IEasingFunction CreateEasing()
            {
                var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };
                easing.Freeze();
                return easing;
            }

            public void OpenAt(int itemIndex, double width)
            {
                ForgetDetachedGaps();
                if (openGap != null && openIndex == itemIndex)
                {
                    return;
                }

                if (openGap != null)
                {
                    BeginClose(openGap, animate: true);
                }

                // Schließt sich an dieser Stelle gerade eine Lücke, wird sie wieder geöffnet (kein Flackern).
                Border gap = FindClosingGapAt(itemIndex) ?? InsertGap(itemIndex);
                closingGaps.Remove(gap);
                openGap = gap;
                openIndex = itemIndex;
                AnimateWidth(gap, width, null);
            }

            public void Close(bool animate)
            {
                ForgetDetachedGaps();
                if (openGap != null)
                {
                    BeginClose(openGap, animate);
                }

                openGap = null;
                openIndex = -1;
            }

            // Ersetzt den Button ohne sichtbaren Sprung durch eine gleich breite Lücke und blendet ihn aus.
            public void ReplaceButton(Button source, double width, bool keepOpen)
            {
                Close(animate: true);

                int childIndex = panel.Children.IndexOf(source);
                if (childIndex < 0)
                {
                    source.Visibility = Visibility.Collapsed;
                    return;
                }

                int itemIndex = 0;
                for (int i = 0; i < childIndex; i++)
                {
                    if (DockDropPosition.IsInsertionTarget(panel.Children[i]))
                    {
                        itemIndex++;
                    }
                }

                var gap = CreateGap(width);
                panel.Children.Insert(childIndex, gap);
                source.Visibility = Visibility.Collapsed;

                if (keepOpen)
                {
                    openGap = gap;
                    openIndex = itemIndex;
                }
                else
                {
                    BeginClose(gap, animate: true);
                }
            }

            public void RemoveAll()
            {
                foreach (var gap in panel.Children.OfType<Border>().Where(DockDropPosition.IsDropGap).ToList())
                {
                    RemoveGap(gap);
                }

                foreach (var gap in closingGaps.Keys)
                {
                    gap.BeginAnimation(FrameworkElement.WidthProperty, null);
                }

                closingGaps.Clear();
                openGap = null;
                openIndex = -1;
            }

            // Das Kategorie-Dock leert beim Öffnen seine Kinder; dabei verschwundene Lücken vergessen.
            private void ForgetDetachedGaps()
            {
                if (openGap != null && !panel.Children.Contains(openGap))
                {
                    openGap = null;
                    openIndex = -1;
                }

                foreach (var gap in closingGaps.Keys.Where(gap => !panel.Children.Contains(gap)).ToList())
                {
                    gap.BeginAnimation(FrameworkElement.WidthProperty, null);
                    closingGaps.Remove(gap);
                }
            }

            private Border? FindClosingGapAt(int itemIndex)
            {
                int seen = 0;
                foreach (UIElement child in panel.Children)
                {
                    if (DockDropPosition.IsInsertionTarget(child))
                    {
                        if (seen == itemIndex)
                        {
                            break;
                        }

                        seen++;
                    }
                    else if (seen == itemIndex && child is Border gap && closingGaps.ContainsKey(gap))
                    {
                        return gap;
                    }
                }

                return null;
            }

            private Border InsertGap(int itemIndex)
            {
                var gap = CreateGap(0);
                panel.Children.Insert(DockDropPosition.GetChildIndex(panel, itemIndex), gap);
                return gap;
            }

            private static Border CreateGap(double width)
            {
                // Unsichtbar und ohne Treffertest: Drag-Ereignisse gehen an das Dock darunter.
                return new Border
                {
                    Width = width,
                    Tag = DockDropPosition.DropGapTag,
                    IsHitTestVisible = false,
                    Focusable = false
                };
            }

            private void BeginClose(Border gap, bool animate)
            {
                if (!animate)
                {
                    closingGaps.Remove(gap);
                    RemoveGap(gap);
                    return;
                }

                DoubleAnimation? animation = null;
                animation = AnimateWidth(gap, 0, () =>
                {
                    // Nur entfernen, wenn die Lücke inzwischen nicht wieder geöffnet wurde.
                    if (closingGaps.TryGetValue(gap, out var current) && ReferenceEquals(current, animation))
                    {
                        closingGaps.Remove(gap);
                        RemoveGap(gap);
                    }
                });
                closingGaps[gap] = animation;
            }

            private void RemoveGap(Border gap)
            {
                gap.BeginAnimation(FrameworkElement.WidthProperty, null);
                panel.Children.Remove(gap);
            }

            private static DoubleAnimation AnimateWidth(Border gap, double to, Action? completed)
            {
                // Ohne From startet die Animation beim aktuellen Wert (auch mitten in einer laufenden Animation).
                var animation = new DoubleAnimation(to, AnimationDuration)
                {
                    EasingFunction = Easing
                };
                if (completed is { } onCompleted)
                {
                    animation.Completed += (_, _) => onCompleted();
                }

                gap.BeginAnimation(FrameworkElement.WidthProperty, animation);
                return animation;
            }
        }

        #endregion

        // Öffnet beim Ziehen über einen Kategorie-Button dessen Kategorie (nur bei Wechsel).
        private void UpdateCategoryForDragPosition(Point dropPosition)
        {
            foreach (var child in DockPanel.Children)
            {
                if (child is not Button button || button.Tag is not DockItem dockItem)
                {
                    continue;
                }

                Rect elementRect = new Rect(button.TranslatePoint(new Point(0, 0), DockPanel), button.RenderSize);
                if (!elementRect.Contains(dropPosition))
                {
                    continue;
                }

                if (dockItem.IsCategory)
                {
                    // Nur neu aufbauen, wenn wirklich eine andere Kategorie geöffnet werden soll; ein erneuter
                    // Aufbau würde während des Ziehens Duplikate des gezogenen Elements erzeugen.
                    if (CategoryDockContainer.Visibility != Visibility.Visible
                        || !string.Equals(CategoryDockContainer.Tag as string, dockItem.Id, StringComparison.Ordinal))
                    {
                        lastDragCategoryId = dockItem.Id;
                        ShowCategoryDockPanel(new StackPanel { Tag = dockItem.Id });
                    }
                }
                else if (lastDragCategoryId != null || isCategoryDockOpen)
                {
                    lastDragCategoryId = null;
                    HideCategoryDockPanel();
                    currentDockStatus &= ~DockStatus.CategoryElementClicked;
                }

                return;
            }
        }

        private void DockPanel_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = GetDropEffect(e.Data);
            e.Handled = true;
            if (e.Effects == DragDropEffects.None)
            {
                return;
            }

            NoteDragActivity();
            ShowDock();
            dockManager.LogMousePositionAndElements(e.GetPosition(DockPanel));

            CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
            currentDockStatus |= DockStatus.DraggingToDock;
            CheckAllConditions();

            Point dropPosition = e.GetPosition(DockPanel);
            UpdateCategoryForDragPosition(dropPosition);
            UpdateDropGap(DockPanel, MainDockGap, e.Data, dropPosition.X);
            DockPanel.Background = (SolidColorBrush)Application.Current.Resources["FeedbackColor"];
        }

        private void DockPanel_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = GetDropEffect(e.Data);
            e.Handled = true;
            if (e.Effects == DragDropEffects.None)
            {
                mainDockGap?.Close(animate: true);
                return;
            }

            NoteDragActivity();
            AutoScrollDuringDrag(MainDockScrollViewer, e);
            Point dropPosition = e.GetPosition(DockPanel);
            UpdateCategoryForDragPosition(dropPosition);
            UpdateDropGap(DockPanel, MainDockGap, e.Data, dropPosition.X);
        }



        private void DockPanel_DragLeave(object sender, DragEventArgs e)
        {
            // DragLeave feuert auch beim Wechsel auf Kindelemente; nur echtes Verlassen behandeln.
            Point position = e.GetPosition(DockPanel);
            if (new Rect(DockPanel.RenderSize).Contains(position))
            {
                ConfirmDragLeaveLater();
                return;
            }

            mainDockGap?.Close(animate: true);

            CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"]; // Visuelles Feedback zurücksetzen Farbe
            currentDockStatus &= ~DockStatus.DraggingToDock;  // Flag zurücksetzen, wenn der Drag-Vorgang das DockPanel verlässt
            CheckAllConditions();

            // Visuelles Feedback zurücksetzen
            var brush = (SolidColorBrush)FindResource("PrimaryColor");
            if (brush != null)
            {
                DockPanel.Background = brush; // Setze auf die ursprüngliche Farbe zurück
            }
        }

        public void CategoryDockContainer_Drop(object sender, DragEventArgs e)
        {
            // Die Lücke bleibt bis nach dem Einfügen stehen: Die Einfügeposition wird mit demselben
            // Layout berechnet wie beim letzten DragOver (Lücken und ausgeblendete Buttons zählen nicht).
            Point dropPosition = e.GetPosition(CategoryDockContainer);

            try
            {
                // Die Kategorie stammt aus dem Tag (gesetzt in ShowCategoryDockPanel), da DragLeave
                // currentOpenCategory bereits geleert haben kann.
                string categoryId = CategoryDockContainer.Tag as string ?? string.Empty;
                if (string.IsNullOrEmpty(categoryId) || categoryId == "KategorieDockContainer")
                {
                    return;
                }

                currentOpenCategory = categoryId;

                if (e.Data.GetDataPresent(DockDropPosition.DockItemButtonFormat))
                {
                    if (e.Data.GetData(DockDropPosition.DockItemButtonFormat) is Button button &&
                        button.Tag is DockItem droppedItem &&
                        !droppedItem.IsCategory)
                    {
                        (VisualTreeHelper.GetParent(button) as Panel)?.Children.Remove(button);
                        droppedItem.Category = categoryId;
                        int insertionIndex = DockDropPosition.FindInsertionIndex(CategoryDockContainer, dropPosition.X);
                        CategoryDockContainer.Children.Insert(insertionIndex, button);
                        dockManager.SaveDockItems(categoryId);
                        RefreshDockNavigation();
                    }
                }
                else if (DockDropPosition.GetDroppedFilePaths(e.Data) is string[] files)
                {
                    AddFilesToCategory(files, dropPosition.X, categoryId);
                    RefreshDockNavigation();
                }
                else if (DockDropPosition.TryGetLinkText(e.Data, out string link))
                {
                    int insertionIndex = DockDropPosition.FindInsertionIndex(CategoryDockContainer, dropPosition.X);
                    var dockItem = new DockItem
                    {
                        FilePath = link,
                        DisplayName = DockDropPosition.GetDisplayName(link),
                        Category = categoryId
                    };
                    dockManager.AddDockItemAt(dockItem, insertionIndex, categoryId, saveChanges: false);
                    dockManager.SaveDockItems(categoryId);
                    RefreshDockNavigation();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CategoryDockContainer_Drop: Fehler aufgetreten - {ex}");
                DialogMessageBox.Show(this, "Das Element konnte nicht zur Kategorie hinzugefügt werden.",
                    "BiMaDock", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                // Lücken sofort entfernen und den gezogenen Button an seiner neuen Position einblenden.
                ResetDropGaps();
                dragLeaveCheckTimer?.Stop();
                e.Handled = true;
                CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
                CheckAllConditions();
            }
        }

        private void AddFilesToCategory(string[] files, double dropX, string categoryId)
        {
            int insertionIndex = DockDropPosition.FindInsertionIndex(CategoryDockContainer, dropX);
            foreach (string file in files)
            {
                var dockItem = new DockItem
                {
                    FilePath = file,
                    DisplayName = DockDropPosition.GetDisplayName(file),
                    Category = categoryId
                };
                dockManager.AddDockItemAt(dockItem, insertionIndex++, categoryId, saveChanges: false);
            }

            dockManager.SaveDockItems(categoryId);
        }

        public void CategoryDockContainer_DragEnter(object sender, DragEventArgs e)
        {
            if (CategoryDockContainer.Tag is string categoryName)
            {
                currentOpenCategory = categoryName;
            }

            CategoryDockContainer_DragOver(sender, e);
        }

        public void CategoryDockContainer_DragOver(object sender, DragEventArgs e)
        {
            if (!dockVisible)
            {
                ShowDock();
            }

            e.Effects = GetDropEffect(e.Data);
            if (e.Effects == DragDropEffects.Move &&
                e.Data.GetData(DockDropPosition.DockItemButtonFormat) is Button button &&
                button.Tag is DockItem droppedItem &&
                droppedItem.IsCategory)
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
            AutoScrollDuringDrag(CategoryDockScrollViewer, e);

            if (e.Effects != DragDropEffects.None)
            {
                NoteDragActivity();
                UpdateDropGap(CategoryDockContainer, CategoryDockGap, e.Data, e.GetPosition(CategoryDockContainer).X);
                CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["FeedbackColor"];
            }
            else
            {
                categoryDockGap?.Close(animate: true);
                CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
            }
        }

        public void CategoryDockContainer_DragLeave(object sender, DragEventArgs e)
        {
            // Wechsel auf Kindelemente ignorieren, um Flackern zu vermeiden.
            if (new Rect(CategoryDockContainer.RenderSize).Contains(e.GetPosition(CategoryDockContainer)))
            {
                ConfirmDragLeaveLater();
                return;
            }

            categoryDockGap?.Close(animate: true);

            if (!isDragging)
            {
                currentOpenCategory = "";
            }
            CategoryDockBorder.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
            CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
        }
    }
}
