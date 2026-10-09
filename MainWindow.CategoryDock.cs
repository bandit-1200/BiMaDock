using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System;

namespace BiMaDock
{
    public partial class MainWindow
    {
        private void CategoryDockContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            currentDockStatus |= DockStatus.CategoryDockHover; // Setzt das CategoryDockHover-Flag
            CheckAllConditions();
        }


        private void CategoryDockContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            currentDockStatus &= ~DockStatus.CategoryDockHover; // Löscht das CategoryDockHover-Flag
            CheckAllConditions();
        }

        private Button? previousButton = null;
        public void CategoryDockContainer_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                return;
            }

            Point mousePosition = e.GetPosition(CategoryDockContainer);
            bool isOverElement = false;

            for (int i = 0; i < CategoryDockContainer.Children.Count; i++)
            {
                if (CategoryDockContainer.Children[i] is Button button)
                {
                    Rect elementRect = new Rect(button.TranslatePoint(new Point(0, 0), CategoryDockContainer), button.RenderSize);
                    if (elementRect.Contains(mousePosition))
                    {
                        isOverElement = true;
                        // Nur beim Betreten eines neuen Elements animieren; keine Button-Referenzen sammeln.
                        if (button != previousButton)
                        {
                            ButtonAnimations.AnimateButtonByChoice(button);
                        }
                        previousButton = button;
                        break;
                    }
                }
            }
            if (!isOverElement)
            {
                previousButton = null;
            }
        }

        public void OpenDockItem(DockItem dockItem)
        {
            if (!string.IsNullOrEmpty(dockItem.FilePath))
            {
                OpenFile(dockItem.FilePath);
            }
            else
            {
                if (isCategoryDockOpen && currentOpenCategory == dockItem.Id)
                {
                    // Kategoriedock schließen
                    CategoryDockContainer.Visibility = Visibility.Collapsed;
                    CategoryDockBorder.Visibility = Visibility.Collapsed;
                    OverlayCanvas.Visibility = Visibility.Collapsed;
                    currentDockStatus &= ~DockStatus.CategoryElementClicked; // Flag zurücksetzen

                    isCategoryDockOpen = false;
                    isCategoryDockOpenID = "";
                }
                else
                {
                    // Kategoriedock öffnen
                    ShowCategoryDockPanel(new StackPanel
                    {
                        Tag = dockItem.Id
                    });







                    currentDockStatus |= DockStatus.CategoryElementClicked; // Flag setzen
                    isCategoryDockOpen = true;
                    isCategoryDockOpenID = dockItem.Id;
                }
            }
            CheckAllConditions();
        }






        public void OpenFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    using var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler beim Öffnen der Datei: {ex.Message}"); // Debug-Ausgabe
                }
            }
            else
            {
                Debug.WriteLine("Fehler: Kein Dateipfad bereitgestellt"); // Debug-Ausgabe
            }
            HideCategoryDockPanel();
            HideDock();
            currentDockStatus = DockStatus.None;


        }

        public void ShowCategoryDockPanel(StackPanel categoryDock)
        {
            previousButton = null;
            CategoryDockContainer.Children.Clear();
            CategoryDockContainer.Visibility = Visibility.Visible;
            CategoryDockBorder.Visibility = Visibility.Visible;
            OverlayCanvas.Visibility = Visibility.Visible;
            Panel.SetZIndex(OverlayCanvas, 1000);
            Panel.SetZIndex(CategoryDockBorder, 0);

            currentDockStatus |= DockStatus.CategoryElementClicked;
            currentOpenCategory = categoryDock.Tag?.ToString() ?? string.Empty;
            CategoryDockContainer.Tag = currentOpenCategory;

            var items = SettingsManager.LoadSettings();
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.Category) && item.Category == currentOpenCategory)
                {
                    // Wird gerade ein Element dieser Kategorie gezogen, den (ausgeblendeten) Original-Button
                    // wiederverwenden statt einen zweiten Button für dasselbe Element zu erzeugen.
                    if (hiddenDragSource is { Tag: DockItem draggedItem } && draggedItem.Id == item.Id && hiddenDragSource.Parent == null)
                    {
                        CategoryDockContainer.Children.Add(hiddenDragSource);
                        continue;
                    }

                    dockManager.AddDockItemAt(item, CategoryDockContainer.Children.Count, currentOpenCategory, saveChanges: false);
                }
            }

            CategoryDockScrollViewer.ScrollToHorizontalOffset(0);
            categoryDockNavigation.Refresh();

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (CategoryDockBorder.ActualWidth > 0)
                {
                    double mainWindowCenterX = Application.Current.MainWindow.ActualWidth / 2;
                    double mainStackPanelCenterX = MainStackPanel.ActualWidth / 2;
                    double elementCenterX = dockManager.mousePositionSave + mainStackPanelCenterX;
                    double categoryDockPositionX = dockManager.mousePositionSave + dockManager.mousePositionSave;
                    // Setze die neue Position
                    CategoryDockBorder.Margin = new Thickness(categoryDockPositionX, 0, 0, 0);

                    double categoryDockCenterX = categoryDockPositionX + (CategoryDockBorder.ActualWidth / 2);
                    double positionRelativeToCenter = categoryDockCenterX - mainWindowCenterX;

                    double overlayPositionX = dockManager.mousePositionSaveleft - 10;
                    double overlayPositionY = 80;

                    Canvas.SetLeft(OverlayCanvasHorizontalLine, overlayPositionX);
                    Canvas.SetTop(OverlayCanvasHorizontalLine, overlayPositionY);
                    Panel.SetZIndex(OverlayCanvasHorizontalLine, 1000); // Höherer Wert bringt es in den Vordergrund

                }
                PositionCategoryDock(currentOpenCategory);
            }, System.Windows.Threading.DispatcherPriority.Loaded);

            // Einblendanimation hinzufügen
            DoubleAnimation fadeInAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.5) // Dauer der Animation
            };

            // Animation starten, wenn das Element sichtbar wird
            CategoryDockContainer.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);

            MainStackPanel.Margin = new Thickness(0);
            categoryHideTimer.Start();
            CheckAllConditions();
        }


        public void HideCategoryDockPanel()
        {
            CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];// Visuelles Feedback zurücksetzen Farbe
            CategoryDockContainer.Visibility = Visibility.Collapsed;
            CategoryDockBorder.Visibility = Visibility.Collapsed; // Sichtbarkeit der CategoryDockBorder ändern
            OverlayCanvas.Visibility = Visibility.Collapsed;

            // MainStackPanel zurücksetzen
            MainStackPanel.Margin = new Thickness(0, 0, 0, 0);

            // Timer stoppen
            categoryHideTimer.Stop();
            isCategoryDockOpen = false;
        }

        private void CategoryDockArea_MouseEnter(object sender, MouseEventArgs e)
        {
            currentDockStatus |= DockStatus.CategoryDockHover;
            dockHideTimer?.Stop();
            categoryHideTimer?.Stop();
        }

        private void CategoryDockArea_MouseLeave(object sender, MouseEventArgs e)
        {
            currentDockStatus &= ~DockStatus.CategoryDockHover;
            CheckAllConditions();
        }

        private void PositionCategoryDock(string categoryId)
        {
            CategoryDockPositioner.Position(
                categoryId,
                DockPanel,
                MainStackPanel,
                MainGrid,
                CategoryDockBorder,
                OverlayCanvasHorizontalLine);
        }

        private void MainDockPreviousButton_Click(object sender, RoutedEventArgs e) =>
            mainDockNavigation.ScrollPrevious();

        private void MainDockNextButton_Click(object sender, RoutedEventArgs e) =>
            mainDockNavigation.ScrollNext();

        private void CategoryDockPreviousButton_Click(object sender, RoutedEventArgs e) =>
            categoryDockNavigation.ScrollPrevious();

        private void CategoryDockNextButton_Click(object sender, RoutedEventArgs e) =>
            categoryDockNavigation.ScrollNext();

        private void MainDockScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            mainDockNavigation.ScrollByWheelDelta(e.Delta);
        }

        private void CategoryDockScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            categoryDockNavigation.ScrollByWheelDelta(e.Delta);
        }

        private void DockScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (ReferenceEquals(sender, MainDockScrollViewer))
            {
                mainDockNavigation.Refresh();
            }
            else if (ReferenceEquals(sender, CategoryDockScrollViewer))
            {
                categoryDockNavigation.Refresh();
            }
        }

        private void RefreshDockNavigation()
        {
            mainDockNavigation.Refresh();
            categoryDockNavigation.Refresh();
        }
    }
}
