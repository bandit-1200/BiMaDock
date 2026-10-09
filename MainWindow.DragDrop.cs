using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System;
using System.Linq;

namespace BiMaDock
{
    public partial class MainWindow
    {
        private Border? currentPlaceholder = null; // Referenz auf den Platzhalter

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

        // Wird nach DoDragDrop aufgerufen und räumt alle Drag-Rückmeldungen auf.
        public void CleanupAfterDrag()
        {
            RemoveDockPanelPlaceholders();
            RemoveCategoryDockPlaceholders();
            lastDragCategoryId = null;

            var primaryBrush = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
            DockPanel.Background = primaryBrush;
            CategoryDockContainer.Background = primaryBrush;

            currentDockStatus &= ~DockStatus.DraggingToDock;
            CheckAllConditions();
        }

        private static readonly SolidColorBrush PlaceholderBrush = CreateFrozenBrush(Colors.LightGray);
        private Border? categoryPlaceholder = null; // Wiederverwendeter Platzhalter im Kategorie-Dock
        private string? lastDragCategoryId = null; // Zuletzt beim Ziehen geöffnete Kategorie

        private static SolidColorBrush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static Border CreatePlaceholder(string tag, double width)
        {
            return new Border
            {
                Background = PlaceholderBrush,
                Opacity = 1.0,
                Height = 60.0, // Höhe des Platzhalters
                Width = width, // Breite des Platzhalters
                Tag = tag
            };
        }

        // Verschiebt den Platzhalter nur, wenn sich die Einfügeposition tatsächlich ändert.
        // FindInsertionIndex zählt nur Buttons, liefert aber einen Index in Children (inklusive Platzhalter).
        private static void MovePlaceholder(Panel panel, Border placeholder, double dropX)
        {
            int targetIndex = DockDropPosition.FindInsertionIndex(panel, dropX);
            int currentIndex = panel.Children.IndexOf(placeholder);
            if (currentIndex >= 0)
            {
                if (targetIndex == currentIndex || targetIndex == currentIndex + 1)
                {
                    return;
                }

                panel.Children.RemoveAt(currentIndex);
                if (targetIndex > currentIndex)
                {
                    targetIndex--;
                }
            }

            panel.Children.Insert(Math.Min(targetIndex, panel.Children.Count), placeholder);
        }

        private void RemoveDockPanelPlaceholders()
        {
            var allPlaceholders = DockPanel.Children.OfType<Border>().Where(border => border.Tag as string == "Placeholder").ToList();
            foreach (var placeholder in allPlaceholders)
            {
                DockPanel.Children.Remove(placeholder);
            }
        }

        private void UpdateDockPanelPlaceholder(double dropX)
        {
            currentPlaceholder ??= CreatePlaceholder("Placeholder", 3);
            MovePlaceholder(DockPanel, currentPlaceholder, dropX);
        }

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
                    if (dockItem.Id != lastDragCategoryId || !isCategoryDockOpen)
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

            ShowDock();
            dockManager.LogMousePositionAndElements(e.GetPosition(DockPanel));

            CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
            currentDockStatus |= DockStatus.DraggingToDock;
            CheckAllConditions();

            Point dropPosition = e.GetPosition(DockPanel);
            UpdateCategoryForDragPosition(dropPosition);
            UpdateDockPanelPlaceholder(dropPosition.X);
            DockPanel.Background = (SolidColorBrush)Application.Current.Resources["FeedbackColor"];
        }

        private void DockPanel_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = GetDropEffect(e.Data);
            e.Handled = true;
            if (e.Effects == DragDropEffects.None)
            {
                RemoveDockPanelPlaceholders();
                return;
            }

            AutoScrollDuringDrag(MainDockScrollViewer, e);
            Point dropPosition = e.GetPosition(DockPanel);
            UpdateCategoryForDragPosition(dropPosition);
            UpdateDockPanelPlaceholder(dropPosition.X);
        }



        private void DockPanel_DragLeave(object sender, DragEventArgs e)
        {
            // DragLeave feuert auch beim Wechsel auf Kindelemente; nur echtes Verlassen behandeln.
            Point position = e.GetPosition(DockPanel);
            if (new Rect(DockPanel.RenderSize).Contains(position))
            {
                return;
            }

            RemoveDockPanelPlaceholders();

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
            RemoveCategoryDockPlaceholders();
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
                MessageBox.Show(this, "Das Element konnte nicht zur Kategorie hinzugefügt werden.",
                    "BiMaDock", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
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

        private void UpdateCategoryDockPlaceholder(Point dropPosition)
        {
            categoryPlaceholder ??= CreatePlaceholder("CategoryPlaceholder", 1);
            MovePlaceholder(CategoryDockContainer, categoryPlaceholder, dropPosition.X);
        }



        private void RemoveCategoryDockPlaceholders()
        {
            var allPlaceholders = CategoryDockContainer.Children.OfType<Border>().Where(border => border.Tag as string == "CategoryPlaceholder").ToList();
            foreach (var placeholder in allPlaceholders)
            {
                CategoryDockContainer.Children.Remove(placeholder);
            }
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
                UpdateCategoryDockPlaceholder(e.GetPosition(CategoryDockContainer));
                CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["FeedbackColor"];
            }
            else
            {
                RemoveCategoryDockPlaceholders();
                CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
            }
        }

        public void CategoryDockContainer_DragLeave(object sender, DragEventArgs e)
        {
            // Wechsel auf Kindelemente ignorieren, um Flackern zu vermeiden.
            if (new Rect(CategoryDockContainer.RenderSize).Contains(e.GetPosition(CategoryDockContainer)))
            {
                return;
            }

            RemoveCategoryDockPlaceholders();

            if (!isDragging)
            {
                currentOpenCategory = "";
            }
            CategoryDockBorder.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
            CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
        }
    }
}
