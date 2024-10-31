using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading; // Für den DispatcherTimer


namespace MyDockApp
{
    public partial class MainWindow : Window
    {
        private DockManager dockManager;
        // private bool isDragging = false;
        public bool dockVisible = true;
        public bool IsDragging => isDragging;
        private Point? dragStartPoint = null;
        private Button? draggedButton = null;
        public bool isDragging = false; // Flag für Dragging
        private DispatcherTimer dockHideTimer;
        private DispatcherTimer categoryHideTimer;
        // private string currentOpenCategory;
        private string currentOpenCategory = "";
        private bool isCategoryMessageShown = false;


        public void SetDragging(bool value)
        {
            isDragging = value;
        }

        public MainWindow()
        {
            InitializeComponent();
            AllowDrop = true;
            Console.WriteLine("Hauptfenster initialisiert."); // Debugging
            dockManager = new DockManager(DockPanel, CategoryDockContainer, this); // Übergeben von CategoryDockContainer
            dockManager.LoadDockItems();
            Console.WriteLine("Dock-Elemente geladen."); // Debugging

            // Timer initialisieren
            dockHideTimer = new DispatcherTimer();
            dockHideTimer.Interval = TimeSpan.FromSeconds(5); // Zeitintervall auf 5 Sekunden setzen
            dockHideTimer.Tick += (s, e) => { HideDock(); };

            // Timer initialisieren
            categoryHideTimer = new DispatcherTimer();
            categoryHideTimer.Interval = TimeSpan.FromSeconds(3); // Zeitintervall auf 5 Sekunden setzen
            categoryHideTimer.Tick += (s, e) => { HideCategoryDockPanel(); };

            this.Closing += (s, e) =>
            {
                // Sicherstellen, dass die aktuelle Kategorie übergeben wird
                string currentCategory = ""; // Hier die aktuelle Kategorie ermitteln
                dockManager.SaveDockItems(currentCategory);
            };

            DockPanel.PreviewMouseLeftButtonDown += DockPanel_MouseLeftButtonDown;
            DockPanel.PreviewMouseMove += DockPanel_MouseMove;
            DockPanel.PreviewMouseLeftButtonUp += DockPanel_MouseLeftButtonUp;
            DockPanel.PreviewGiveFeedback += DockPanel_PreviewGiveFeedback; // Ereignis hinzufügen
            DockPanel.DragEnter += DockPanel_DragEnter;
            DockPanel.DragLeave += DockPanel_DragLeave;
            DockPanel.Drop += dockManager.DockPanel_Drop;

            this.Loaded += (s, e) =>
            {
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var screenHeight = SystemParameters.PrimaryScreenHeight;
                this.Left = (screenWidth / 2) - (this.Width / 2);
                this.Top = 0; // Fenster am oberen Bildschirmrand positionieren
                this.MouseMove += CheckMousePosition; // Bestätigen, dass die Maus sich bewegt
                DockPanel.MouseEnter += DockPanel_MouseEnter;
                DockPanel.MouseLeave += DockPanel_MouseLeave;
                DockPanel.DragEnter += (s, e) =>
                {
                    e.Effects = DragDropEffects.All;
                    if (!dockVisible) ShowDock();
                };

                // Periodische Überprüfung, ob die Maus über einem der Docks ist
                var hoverCheckTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                hoverCheckTimer.Tick += (s, e) => CheckMouseHover();
                hoverCheckTimer.Start();
            };

            DockPanel.MouseRightButtonDown += (s, e) =>
            {
                OpenMenuItem.Visibility = Visibility.Collapsed;
                DeleteMenuItem.Visibility = Visibility.Collapsed;
                EditMenuItem.Visibility = Visibility.Collapsed;
                DockContextMenu.IsOpen = true;
                if (!DockContextMenu.IsOpen)
                {
                    ShowDock(); // Dock sichtbar halten
                }
            };

            // Registriere die Event-Handler für das Kategoriedock
            CategoryDockContainer.AllowDrop = true;
            CategoryDockContainer.Drop += CategoryDockContainer_Drop;
            CategoryDockContainer.DragEnter += CategoryDockContainer_DragEnter;
            CategoryDockContainer.DragLeave += CategoryDockContainer_DragLeave;
            CategoryDockContainer.PreviewMouseLeftButtonDown += CategoryDockContainer_PreviewMouseLeftButtonDown;
            CategoryDockContainer.MouseMove += CategoryDockContainer_MouseMove;





            var categoryDockContainer = this.FindName("CategoryDockContainer") as StackPanel;
            if (categoryDockContainer != null)
            {
                dockManager.InitializeCategoryDockContainer(categoryDockContainer);
            }
            else
            {
                Console.WriteLine("CategoryDockContainer konnte nicht gefunden werden");
            }
        }
        // Weitere Initialisierung

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            dockManager.Open_Click(sender, e);
        }

        private void CheckMouseHover()
        {
            if (DockPanel != null && CategoryDockContainer != null)
            {
                var mousePosDock = Mouse.GetPosition(DockPanel);
                var dockBounds = new Rect(DockPanel.TranslatePoint(new Point(), this), DockPanel.RenderSize); // Grenzen des Haupt-Docks

                var mousePosCategory = Mouse.GetPosition(CategoryDockContainer);
                var categoryBounds = new Rect(CategoryDockContainer.TranslatePoint(new Point(), this), CategoryDockContainer.RenderSize); // Grenzen des Kategorie-Docks

                if (dockBounds.Contains(mousePosDock) || categoryBounds.Contains(mousePosCategory) || isDragging)
                {
                    // Console.WriteLine($"Mouse over DockPanel or CategoryDockContainer or Drag Start at {DateTime.Now}"); // Debug-Ausgabe

                    // Timer neu starten, wenn die Maus über einem der Docks ist oder ein Draggen erkannt wird
                    dockHideTimer.Stop();
                    dockHideTimer.Start();
                    // Console.WriteLine($"dockHideTimer neu gestartet: {DateTime.Now}"); // Debug-Ausgabe

                    categoryHideTimer.Stop();
                    categoryHideTimer.Start();
                    // Console.WriteLine($"categoryHideTimer neu gestartet: {DateTime.Now}"); // Debug-Ausgabe

                    // Beide Docks sichtbar machen
                    DockPanel.Visibility = Visibility.Visible;
                    CategoryDockContainer.Visibility = Visibility.Visible;
                }
                else
                {
                    // Console.WriteLine($"Mouse not over DockPanel or CategoryDockContainer at {DateTime.Now}"); // Debug-Ausgabe

                    // Timer stoppen und Countdown für das Ausblenden der Docks starten
                    dockHideTimer.Start();
                    categoryHideTimer.Start();
                }
            }
            else
            {
                // Console.WriteLine($"Fehler: DockPanel oder CategoryDockContainer ist null at {DateTime.Now}"); // Debug-Ausgabe
            }
        }



        private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("Mouse Down Event ausgelöst"); // Debugging
            isDragging = true; // Dragging-Flag setzen
            var originalSource = e.OriginalSource as FrameworkElement;
            while (originalSource != null && !(originalSource is Button))
            {
                originalSource = originalSource.Parent as FrameworkElement;
            }
            if (originalSource is Button button)
            {
                dragStartPoint = e.GetPosition(DockPanel);
                draggedButton = button;
                Console.WriteLine("Drag Start: " + draggedButton.Tag); // Debugging
            }
            else
            {
                Console.WriteLine("Kein Button als Quelle gefunden"); // Debugging
            }
        }


        private void DockPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            Console.WriteLine("Mouse entered DockPanel");  // Debug-Ausgabe

            // Timer neu starten, wenn die Maus über dem Dock ist
            dockHideTimer.Stop();
            dockHideTimer.Start();

            categoryHideTimer.Stop();
            categoryHideTimer.Start();
        }



        private void DockPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            Console.WriteLine("Mouse left DockPanel");  // Debug-Ausgabe

            // Timer stoppen, wenn die Maus das Dock verlässt
            dockHideTimer.Stop();
            categoryHideTimer.Stop();
        }













        public void ShowDock()
        {
            if (!dockVisible)
            {
                dockVisible = true;
                var slideAnimation = new ThicknessAnimation
                {
                    From = new Thickness(0, -DockPanel.ActualHeight + 5, 0, 0),  // Startposition der Animation (5 Pixel sichtbar)
                    To = new Thickness(0, 0, 0, 0),  // Endposition der Animation (sichtbar)
                    Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },  // Füge eine sanfte Übergangsanimation hinzu
                    FillBehavior = FillBehavior.HoldEnd
                };
                slideAnimation.Completed += (s, e) =>
                {
                    DockPanel.Margin = new Thickness(0, 0, 0, 0);
                    Console.WriteLine("Dock vollständig eingeblendet"); // Debugging
                };
                DockPanel.BeginAnimation(FrameworkElement.MarginProperty, slideAnimation);
            }
            else
            {
                // Console.WriteLine("Dock ist bereits sichtbar, keine Animation"); // Debugging
            }
        }

        public void InitializeCategoryDockContainer(StackPanel container)
        {
            CategoryDockContainer = container;
        }




        private void CheckMousePosition(object sender, MouseEventArgs e)
        {
            if (DockPanel != null && e != null) // Sicherstellen, dass DockPanel und e nicht null sind
            {
                var mousePos = Mouse.GetPosition(DockPanel);
                var dockBounds = new Rect(DockPanel.TranslatePoint(new Point(), this), DockPanel.RenderSize); // Grenzen des Docks

                if (dockBounds.Contains(mousePos))  // Überprüfen, ob die Maus sich innerhalb der Grenzen des Docks befindet
                {
                    Console.WriteLine("Mouse over DockPanel");  // Debug-Ausgabe

                    // Timer neu starten, wenn die Maus über dem Dock ist
                    dockHideTimer.Stop();
                    dockHideTimer.Start();

                    categoryHideTimer.Stop();
                    categoryHideTimer.Start();
                }
                else
                {
                    Console.WriteLine("Mouse not over DockPanel");  // Debug-Ausgabe
                                                                    // HideDock();

                }
            }
            else
            {
                Console.WriteLine("Fehler: DockPanel oder EventArgs sind null.");  // Debug-Ausgabe
            }
        }



        private void CategoryDockContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            Console.WriteLine("Mouse entered CategoryDockContainer"); // Debug-Ausgabe
            dockHideTimer.Stop();
            dockHideTimer.Start();
            categoryHideTimer.Stop();
            categoryHideTimer.Start();
        }

        private void CategoryDockContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            Console.WriteLine("Mouse left CategoryDockContainer"); // Debug-Ausgabe
            dockHideTimer.Start();
            categoryHideTimer.Start();
        }
        public void CategoryDockContainer_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragStartPoint.HasValue && draggedButton != null)
            {
                Point position = e.GetPosition(CategoryDockContainer);
                Vector diff = dragStartPoint.Value - position;

                if (e.LeftButton == MouseButtonState.Pressed &&
                    (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                     Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    Console.WriteLine($"Dragging: {draggedButton.Tag}, Position: {position}"); // Debugging
                    DragDrop.DoDragDrop(draggedButton, new DataObject(DataFormats.Serializable, draggedButton), DragDropEffects.Move);
                    dragStartPoint = null;
                    draggedButton = null;
                    isDragging = false; // Setze Dragging-Flag zurück
                }
            }
        }




        public void CategoryDockContainer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("CategoryDockContainer_PreviewMouseLeftButtonDown aufgerufen"); // Debugging

            if (sender is StackPanel categoryDock)
            {
                dragStartPoint = e.GetPosition(categoryDock);
                if (e.OriginalSource is FrameworkElement element && element.DataContext is DockItem)
                {
                    draggedButton = element as Button;
                    Console.WriteLine($"Drag start point gesetzt: {dragStartPoint}, Element: {draggedButton?.Tag}"); // Debugging
                }
            }
        }











        public void HideDock()
        {
            if (dockVisible)
            {
                dockVisible = false;
                var slideAnimation = new ThicknessAnimation
                {
                    From = new Thickness(0, 0, 0, 0),  // Startposition der Animation (sichtbar)
                    To = new Thickness(0, -DockPanel.ActualHeight + 5, 0, 0),  // Endposition der Animation (5 Pixel sichtbar lassen)
                    Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },  // Füge eine sanfte Übergangsanimation hinzu
                    FillBehavior = FillBehavior.HoldEnd
                };
                slideAnimation.Completed += (s, e) =>
                {
                    DockPanel.Margin = new Thickness(0, -DockPanel.ActualHeight + 5, 0, 0);
                    Console.WriteLine("Dock teilweise ausgeblendet, 5 Pixel sichtbar"); // Debugging
                };
                DockPanel.BeginAnimation(FrameworkElement.MarginProperty, slideAnimation);
            }
            else
            {
                // Console.WriteLine("Dock ist bereits unsichtbar, keine Animation"); // Debugging
            }
        }




        private void DockPanel_PreviewGiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            if (draggedButton != null)
            {
                var mousePosition = Mouse.GetPosition(DockPanel);

                if (mousePosition.X >= 0 && mousePosition.X <= DockPanel.ActualWidth &&
                    mousePosition.Y >= 0 && mousePosition.Y <= DockPanel.ActualHeight)
                {
                    Console.WriteLine($"Dragging: {draggedButton.Tag}, Effects: {e.Effects}, Mouse Position: {mousePosition}"); // Debugging

                    var hitTestResult = VisualTreeHelper.HitTest(DockPanel, mousePosition);
                    if (hitTestResult != null)
                    {
                        var overElement = hitTestResult.VisualHit as UIElement;
                        if (overElement != null)
                        {
                            if (overElement is Button button && button.Tag is DockItem dockItem)
                            {
                                Console.WriteLine($"Über Element: {dockItem.DisplayName}, IsCategory: {dockItem.IsCategory}, Mouse Position: {mousePosition}"); // Debugging
                            }
                            else
                            {
                                Console.WriteLine("Über einem unbekannten Element oder kein DockItem"); // Debugging
                            }
                        }
                        else
                        {
                            Console.WriteLine("Keine Übereinstimmung mit einem Element im DockPanel"); // Debugging
                        }
                    }
                    else
                    {
                        Console.WriteLine("HitTestResult ist null"); // Debugging
                    }
                }
                else
                {
                    Console.WriteLine("Mausposition außerhalb der Grenzen des DockPanels"); // Debugging
                }
            }
            e.UseDefaultCursors = false;
        }









        private void DockPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragStartPoint.HasValue && draggedButton != null)
            {
                Point position = e.GetPosition(DockPanel);
                Vector diff = dragStartPoint.Value - position;

                if (e.LeftButton == MouseButtonState.Pressed &&
                    (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                     Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    Console.WriteLine($"Dragging: {draggedButton.Tag}, Position: {position}"); // Debugging
                    DragDrop.DoDragDrop(draggedButton, new DataObject(DataFormats.Serializable, draggedButton), DragDropEffects.Move);
                    dragStartPoint = null;
                    draggedButton = null;
                    isDragging = false; // Setze Dragging-Flag zurück
                }
            }
            else
            {
                Point mousePosition = e.GetPosition(DockPanel);
                bool isOverElement = false;

                for (int i = 0; i < DockPanel.Children.Count; i++)
                {
                    if (DockPanel.Children[i] is Button button)
                    {
                        Rect elementRect = new Rect(button.TranslatePoint(new Point(0, 0), DockPanel), button.RenderSize);

                        if (elementRect.Contains(mousePosition))
                        {
                            Console.WriteLine($"Maus über Element: {button.Tag}, Position: {mousePosition}"); // Debugging
                            isOverElement = true;

                            // if (button.Tag is DockItem dockItem && dockItem.IsCategory)
                            // {
                            //     Console.WriteLine("Kategorie-Element erkannt>: " + dockItem.DisplayName); // Debugging
                            //     ShowCategoryDockPanel(new StackPanel
                            //     {
                            //         Name = dockItem.DisplayName,

                            //         // Children = { new Button { Content = $"Kategorie: {dockItem.DisplayName}", Width = 100, Height = 50 } }
                            //     });
                            // }

                            break;
                        }
                    }
                }

                if (!isOverElement)
                {
                    Console.WriteLine($"Maus über Dock ohne Element, Position: {mousePosition}"); // Debugging
                }
            }
        }




        private void DockPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = null;
            draggedButton = null;
            isDragging = false; // Dragging-Flag zurücksetzen
            Console.WriteLine("Drag End"); // Debugging

            // Dock wieder ausblenden, falls die Maus außerhalb ist
            var mousePos = Mouse.GetPosition(this);
            var screenPos = PointToScreen(mousePos);
            if (screenPos.Y > this.Height + 10)
            {
                // HideDock();
            }
        }



        private void DockPanel_DragEnter(object sender, DragEventArgs e)
        {
            Console.WriteLine("DockPanel_DragEnter aufgerufen"); // Debug-Ausgabe

            if (e.Data.GetDataPresent(DataFormats.Serializable) && DockPanel != null)
            {
                e.Effects = DragDropEffects.Move;
                DockPanel.Background = new SolidColorBrush(Colors.LightGreen); // Visuelles Feedback
                Console.WriteLine("Element über dem Hauptdock erkannt"); // Debug-Ausgabe
            }
            else
            {
                e.Effects = DragDropEffects.None;
                Console.WriteLine("Kein Element erkannt oder DockPanel ist null"); // Debug-Ausgabe
            }
        }

        private void DockPanel_DragLeave(object sender, DragEventArgs e)
        {
            Console.WriteLine("DockPanel_DragLeave aufgerufen"); // Debug-Ausgabe

            // Visuelles Feedback zurücksetzen
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFA500");
            if (brush != null)
            {
                DockPanel.Background = brush; // Setze auf Orange zurück
                Console.WriteLine("Element hat das Hauptdock verlassen und Hintergrund zurückgesetzt"); // Debug-Ausgabe
            }
        }




        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


        public void UpdateDockItemLocation(Button button)
        {
            var dockItem = button.Tag as DockItem;
            if (dockItem != null)
            {
                // Aktuelle Kategorie ermitteln
                string currentCategory = dockItem.Category;

                // Aktualisiere die Position des Dock-Items
                dockManager.UpdateDockItemLocation(button, currentCategory); // currentCategory mitgeben
                Console.WriteLine($"DockItem {dockItem.DisplayName} aktualisiert und gespeichert"); // Debug-Ausgabe
            }
            else
            {
                Console.WriteLine("DockItem ist null"); // Debug-Ausgabe
            }
        }



        public void CategoryDockContainer_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Serializable) && !isCategoryMessageShown)
            {
                var button = e.Data.GetData(DataFormats.Serializable) as Button;
                if (button != null)
                {
                    var droppedItem = button.Tag as DockItem;
                    if (droppedItem != null && !string.IsNullOrEmpty(currentOpenCategory))
                    {
                        // Überprüfung auf Kategorie
                        if (droppedItem.IsCategory)
                        {
                            MessageBox.Show("Kategorie-Elemente können nicht in das Kategorie-Dock verschoben werden.", "Verschieben nicht erlaubt", MessageBoxButton.OK, MessageBoxImage.Information);
                            isCategoryMessageShown = true; // Nachricht wurde gezeigt
                            return; // Abbrechen, wenn es eine Kategorie ist
                        }

                        // Überprüfen, ob das Element bereits einer anderen Kategorie zugewiesen ist
                        if (string.IsNullOrEmpty(droppedItem.Category) || droppedItem.Category == currentOpenCategory)
                        {
                            droppedItem.Category = currentOpenCategory;

                            // Füge das Element dem Kategorie-Dock hinzu
                            CategoryDockContainer.Children.Add(button);
                            CategoryDockContainer.Background = new SolidColorBrush(Colors.Transparent); // Visuelles Feedback zurücksetzen

                            // Aktualisiere die interne Struktur oder Daten, falls nötig
                            UpdateDockItemLocation(button);

                            // Dock-Items speichern
                            dockManager.SaveDockItems(currentOpenCategory); // Verwende die gespeicherte Kategorie
                        }
                    }
                }
            }
            isCategoryMessageShown = false; // Nachricht-Flag zurücksetzen
        }

        public void CategoryDockContainer_DragEnter(object sender, DragEventArgs e)
        {
            Console.WriteLine("CategoryDockContainer_DragEnter aufgerufen"); // Debug-Ausgabe

            if (e.Data.GetDataPresent(DataFormats.Serializable))
            {
                e.Effects = DragDropEffects.Move;
                CategoryDockContainer.Background = new SolidColorBrush(Colors.LightGreen); // Visuelles Feedback
                Console.WriteLine("Element über dem Kategoriedock erkannt"); // Debug-Ausgabe

                // Den Tag der geöffneten Kategorie lesen
                var categoryName = CategoryDockContainer.Tag as string;
                if (!string.IsNullOrEmpty(categoryName))
                {
                    currentOpenCategory = categoryName;
                    Console.WriteLine($"Geöffnete Kategorie: {currentOpenCategory}"); // Debug-Ausgabe
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
                Console.WriteLine("Kein Button erkannt"); // Debug-Ausgabe
            }
        }



        public void CategoryDockContainer_DragLeave(object sender, DragEventArgs e)
        {
            Console.WriteLine("CategoryDockContainer_DragLeave aufgerufen"); // Debug-Ausgabe

            // Kategorie-Status nur zurücksetzen, wenn keine anderen Elemente gerade gezogen werden
            if (draggedButton == null)
            {
                currentOpenCategory = "";
                Console.WriteLine("Geöffnete Kategorie zurückgesetzt"); // Debug-Ausgabe
            }

            CategoryDockContainer.Background = new SolidColorBrush(Colors.Transparent); // Visuelles Feedback zurücksetzen
            Console.WriteLine("Element hat das Kategoriedock verlassen"); // Debug-Ausgabe
        }











        public void OpenDockItem(DockItem dockItem)
        {
            if (!string.IsNullOrEmpty(dockItem.FilePath))
            {
                Console.WriteLine($"OpenDockItem aufgerufen, filePath: {dockItem.FilePath}"); // Debug-Ausgabe
                OpenFile(dockItem.FilePath);
            }
            else
            {
                Console.WriteLine("OpenDockItem aufgerufen, Kategorie"); // Debug-Ausgabe
                ShowCategoryDockPanel(new StackPanel
                {
                    Name = dockItem.DisplayName,

                    // Children = { new Button { Content = $"Kategorie: {dockItem.DisplayName}", Width = 100, Height = 50 } }
                });
            }
        }



        public void OpenFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                    Console.WriteLine($"Dateipfad: {filePath}"); // Debug-Ausgabe
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fehler beim Öffnen der Datei: {ex.Message}"); // Debug-Ausgabe
                }
            }
            else
            {
                Console.WriteLine("Fehler: Kein Dateipfad bereitgestellt"); // Debug-Ausgabe
            }
        }



        private void Delete_Click(object sender, RoutedEventArgs e)

        {
            Console.WriteLine("Delete_Click aufgerufen");
            if (DockContextMenu.PlacementTarget is Button button && button.Tag is DockItem dockItem)
            {
                Console.WriteLine($"Delete_Click aufgerufen, filePath: {dockItem.FilePath}"); // Debug-Ausgabe

                // Hier ermitteln wir die Kategorie, die gelöscht werden soll
                string currentCategory = dockItem.Category;

                // Übergabe der Kategorie an RemoveDockItem
                dockManager.RemoveDockItem(button, currentCategory);

                Console.WriteLine("Element gelöscht und Dock aktualisiert"); // Debug-Ausgabe
            }
            else
            {
                Console.WriteLine("Fehler: DockContextMenu.PlacementTarget ist kein Button oder button.Tag ist kein DockItem"); // Debug-Ausgabe
            }
        }








        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            var inputDialog = new InputDialog("Kategorie erstellen", "Bitte geben Sie den Namen der Kategorie ein:");
            if (inputDialog.ShowDialog() == true)
            {
                string categoryName = inputDialog.Answer;
                dockManager.AddCategoryItem(categoryName);
            }
        }



        public void ShowCategoryDockPanel(StackPanel categoryDock)
        {
            Console.WriteLine("ShowCategoryDockPanel - Kategorie-Element erkannt: " + categoryDock.Name); // Debugging des Kategorienamens
            Console.WriteLine("ShowCategoryDockPanel aufgerufen"); // Debugging
                                                                   // Aktuelle Kategorie speichern und Tag setzen
            currentOpenCategory = categoryDock.Name;
            CategoryDockContainer.Tag = currentOpenCategory;
            Console.WriteLine($"Aktuelle Kategorie gesetzt auf: {currentOpenCategory}"); // Debugging
                                                                                         // Kategorie-Dock leeren
            CategoryDockContainer.Children.Clear();
            // Kategorie-Dock hinzufügen
            CategoryDockContainer.Children.Add(categoryDock);
            CategoryDockContainer.Visibility = Visibility.Visible; // Sichtbarkeit der CategoryDockContainer setzen
            CategoryDockBorder.Visibility = Visibility.Visible; // Sichtbarkeit der CategoryDockBorder setzen

            // Elemente der `Docksettings`-Liste überprüfen
            var items = SettingsManager.LoadSettings();
            foreach (var item in items)
            {
                Console.WriteLine($"Überprüfe Element: {item.DisplayName} mit Kategorie: {item.Category}"); // Debugging
                if (!string.IsNullOrEmpty(item.Category) && item.Category == currentOpenCategory)
                {
                    dockManager.AddDockItemAt(item, CategoryDockContainer.Children.Count, currentOpenCategory); // Event-Handler werden in AddDockItemAt gesetzt
                }
            }

            // Dynamische Breite des Kategorie-Docks setzen
            CategoryDockContainer.Width = double.NaN; // Automatische Breite basierend auf Inhalten
            CategoryDockContainer.VerticalAlignment = VerticalAlignment.Top;

            // MainStackPanel nicht nach unten verschieben, um Platz zu schaffen
            MainStackPanel.Margin = new Thickness(0);
            // Timer starten
            categoryHideTimer.Start();
            Console.WriteLine("CategoryDockContainer ist jetzt sichtbar, MainStackPanel neu positioniert."); // Debugging
        }

        public void HideCategoryDockPanel()
        {
            Console.WriteLine("HideCategoryDockPanel aufgerufen"); // Debugging
            CategoryDockContainer.Visibility = Visibility.Collapsed;
            CategoryDockBorder.Visibility = Visibility.Collapsed; // Sichtbarkeit der CategoryDockBorder ändern

            // MainStackPanel zurücksetzen
            MainStackPanel.Margin = new Thickness(0, 0, 0, 0);

            // Timer stoppen
            categoryHideTimer.Stop();
            Console.WriteLine("CategoryDockContainer ausgeblendet, MainStackPanel neu positioniert."); // Debugging
        }






        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (DockContextMenu.PlacementTarget is Button button && button.Tag is DockItem dockItem)
            {
                Console.WriteLine("Edit_Click aufgerufen, filePath: " + dockItem.FilePath); // Debug-Ausgabe
                EditPropertiesWindow editWindow = new EditPropertiesWindow
                {
                    Owner = this,
                    NameTextBox = { Text = System.IO.Path.GetFileNameWithoutExtension(dockItem.FilePath) },
                    PathTextBox = { Text = dockItem.FilePath }
                };

                if (editWindow.ShowDialog() == true)
                {
                    Console.WriteLine("EditPropertiesWindow Dialog result: true"); // Debug-Ausgabe
                    string newName = editWindow.NameTextBox.Text;
                    string newPath = editWindow.PathTextBox.Text;
                    if (!string.IsNullOrEmpty(newName) && !string.IsNullOrEmpty(newPath))
                    {
                        Console.WriteLine("Neuer Name: " + newName + ", Neuer Pfad: " + newPath); // Debug-Ausgabe
                        var icon = IconHelper.GetIcon(newPath);
                        var image = new Image
                        {
                            Source = icon,
                            Width = 32,
                            Height = 32,
                            Margin = new Thickness(5)
                        };
                        var textBlock = new TextBlock
                        {
                            Text = newName,
                            TextAlignment = TextAlignment.Center,
                            TextWrapping = TextWrapping.Wrap,
                            Width = 60,
                            Margin = new Thickness(5)
                        };
                        var stackPanel = new StackPanel
                        {
                            Orientation = Orientation.Vertical,
                            Width = 70
                        };
                        stackPanel.Children.Add(image);
                        stackPanel.Children.Add(textBlock);
                        button.Content = stackPanel;
                        dockItem.DisplayName = newName;
                        dockItem.FilePath = newPath;
                        button.Tag = dockItem;

                        // Übergabe der aktuellen Kategorie an SaveDockItems
                        dockManager.SaveDockItems(dockItem.Category);

                        Console.WriteLine("Dock-Elemente gespeichert"); // Debug-Ausgabe
                    }
                    else
                    {
                        Console.WriteLine("Ungültiger Name oder Pfad"); // Debug-Ausgabe
                    }
                }
                else
                {
                    Console.WriteLine("EditPropertiesWindow Dialog result: false"); // Debug-Ausgabe
                }
            }
            else
            {
                Console.WriteLine("Fehler: DockContextMenu.PlacementTarget ist kein Button oder button.Tag ist kein DockItem"); // Debug-Ausgabe
            }
        }

    }

}