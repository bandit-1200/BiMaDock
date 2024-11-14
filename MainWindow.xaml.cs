using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading; // Für den DispatcherTimer



namespace BiMaDock
{
    public partial class MainWindow : Window
    {
        // private GlobalMouseHook mouseHook;  // Deklariere die private Variable für den Hook

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
        public bool isCategoryDockOpen = false;
        public string isCategoryDockOpenID = "";

        private bool isCategoryMessageShown = false;

        private Border? currentPlaceholder = null; // Referenz auf den Platzhalter
        private int currentPlaceholderIndex = -1; // Index des Platzhalters, der die aktuelle Position speichert

        public void SetDragging(bool value)
        {
            isDragging = value;
        }

        [Flags]
        public enum DockStatus
        {
            None = 0,
            MainDockHover = 1,
            CategoryDockHover = 2,
            ContextMenuOpen = 4,
            CategoryElementClicked = 8,
            DraggingToDock = 16

        }

        public DockStatus currentDockStatus = DockStatus.None;

        public MainWindow()
        {
            InitializeComponent();
            CheckAutostart();
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            this.Width = screenWidth * 0.8;  // 80% der Bildschirmbreite
            // GlobalMouseHook mouseHook = new GlobalMouseHook(this); // 'this' bezieht sich auf das MainWindow

            // mouseHook = new GlobalMouseHook();
            // mouseHook.Start();


            ButtonAnimations.LoadSettings();
            AllowDrop = true;
            Console.WriteLine("Hauptfenster initialisiert."); // Debugging
            dockManager = new DockManager(DockPanel, CategoryDockContainer, this); // Übergeben von CategoryDockContainer
            dockManager.LoadDockItems();
            Console.WriteLine("Dock-Elemente geladen."); // Debugging

            // Timer initialisieren
            dockHideTimer = new DispatcherTimer();
            dockHideTimer.Interval = TimeSpan.FromSeconds(0.3); // Zeitintervall auf 5 Sekunden setzen
            dockHideTimer.Tick += (s, e) => { HideDock(); };

            // Timer initialisieren
            categoryHideTimer = new DispatcherTimer();
            categoryHideTimer.Interval = TimeSpan.FromSeconds(0.3); // Zeitintervall auf 5 Sekunden setzen
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
            // DockPanel.PreviewGiveFeedback += DockPanel_PreviewGiveFeedback; 
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
                    Interval = TimeSpan.FromSeconds(0.2)
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
                if (DockContextMenu.IsOpen)
                {
                    // ShowDock(); // Dock sichtbar halten
                    currentDockStatus |= DockStatus.ContextMenuOpen;
                    CheckAllConditions();
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



        // Methode zum Öffnen des Einstellungsfensters
        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog(); // Modal anzeigen
        }



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
                    // Timer stoppen, wenn die Maus über einem der Docks ist oder ein Draggen erkannt wird
                    dockHideTimer.Stop();
                    categoryHideTimer.Stop();
                    // Beide Docks sichtbar machen
                    ShowDock();
                    CategoryDockContainer.Visibility = Visibility.Visible;
                }
                else
                {
                    // Timer starten und Countdown für das Ausblenden der Docks starten
                    // dockHideTimer.Start();
                    // categoryHideTimer.Start();
                }
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
                Console.WriteLine("DockPanel_MouseLeftButtonDown: Drag Start: " + draggedButton.Tag); // Debugging
            }
            else
            {
                Console.WriteLine("DockPanel_MouseLeftButtonDown: Kein Button als Quelle gefunden"); // Debugging
            }
        }

        private void DockPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            currentDockStatus |= DockStatus.MainDockHover; // Setzt das MainDockHover-Flag
            CheckAllConditions();
        }


        private void DockPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            currentDockStatus &= ~DockStatus.MainDockHover; // Löscht das MainDockHover-Flag
            // currentDockStatus &= ~DockStatus.DraggingToDock;
            CheckAllConditions();

            if (!DockContextMenu.IsOpen)
            {
                // ShowDock(); // Dock sichtbar halten
                currentDockStatus &= ~DockStatus.ContextMenuOpen;
                CheckAllConditions();
            }
        }




        public void ShowDock()
        {
            if (!dockVisible)
            {
                dockVisible = true;
                var duration = TimeSpan.FromMilliseconds(100);
                var slideAnimation = new ThicknessAnimation
                {
                    From = new Thickness(0, -DockPanel.ActualHeight + 5, 0, 0),
                    To = new Thickness(0, 0, 0, 0),
                    Duration = duration,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
                    FillBehavior = FillBehavior.HoldEnd
                };
                slideAnimation.Completed += (s, e) =>
                {
                    DockPanel.Margin = new Thickness(0, 0, 0, 0);
                    Console.WriteLine("Dock vollständig eingeblendet"); // Debugging
                };



                // Endkappen einblenden
                var endCapAnimation = new DoubleAnimation
                {
                    To = 0,
                    Duration = duration,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                if (LeftEndCap.RenderTransform == null)
                {
                    LeftEndCap.RenderTransform = new TranslateTransform();
                }
                if (RightEndCap.RenderTransform == null)
                {
                    RightEndCap.RenderTransform = new TranslateTransform();
                }
                DockPanel.BeginAnimation(FrameworkElement.MarginProperty, slideAnimation);
                LeftEndCap.RenderTransform.BeginAnimation(TranslateTransform.YProperty, endCapAnimation);
                RightEndCap.RenderTransform.BeginAnimation(TranslateTransform.YProperty, endCapAnimation);
            }
        }






        public void HideDock()
        {
            currentDockStatus = DockStatus.None;
            if (dockVisible)
            {
                dockVisible = false;
                var duration = TimeSpan.FromMilliseconds(100);
                var toValue = -DockPanel.ActualHeight + 5;
                var slideAnimation = new ThicknessAnimation
                {
                    From = new Thickness(0, 0, 0, 0),
                    To = new Thickness(0, -DockPanel.ActualHeight + toValue, 0, 0),
                    Duration = duration,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
                    FillBehavior = FillBehavior.HoldEnd
                };

                slideAnimation.Completed += (s, e) =>
                {
                    DockPanel.Margin = new Thickness(0, -DockPanel.ActualHeight + toValue, 0, 0);
                    Console.WriteLine("Dock teilweise ausgeblendet, 5 Pixel sichtbar"); // Debugging
                };



                // Endkappen ausblenden
                var endCapAnimation = new DoubleAnimation
                {
                    To = -DockPanel.ActualHeight + toValue,
                    Duration = duration,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                if (LeftEndCap.RenderTransform == null)
                {
                    LeftEndCap.RenderTransform = new TranslateTransform();
                }
                if (RightEndCap.RenderTransform == null)
                {
                    RightEndCap.RenderTransform = new TranslateTransform();
                }
                DockPanel.BeginAnimation(FrameworkElement.MarginProperty, slideAnimation);
                LeftEndCap.RenderTransform.BeginAnimation(TranslateTransform.YProperty, endCapAnimation);
                RightEndCap.RenderTransform.BeginAnimation(TranslateTransform.YProperty, endCapAnimation);
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
                    // dockHideTimer.Stop();
                    // dockHideTimer.Start();

                    // categoryHideTimer.Stop();
                    // categoryHideTimer.Start();
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
            currentDockStatus |= DockStatus.CategoryDockHover; // Setzt das CategoryDockHover-Flag
            // currentDockStatus &= ~DockStatus.CategoryElementClicked; // Löscht das CategoryElementClicked-Flag
            CheckAllConditions();
        }



        private void CategoryDockContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            currentDockStatus &= ~DockStatus.CategoryDockHover; // Löscht das CategoryDockHover-Flag
            CheckAllConditions();
        }

        private void DockPanel_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            currentDockStatus |= DockStatus.ContextMenuOpen; // Setzt das ContextMenuOpen-Flag
            OpenMenuItem.Visibility = Visibility.Collapsed;
            DeleteMenuItem.Visibility = Visibility.Collapsed;
            EditMenuItem.Visibility = Visibility.Collapsed;
            DockContextMenu.IsOpen = true;
            CheckAllConditions();
        }

        private void DockContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            currentDockStatus &= ~DockStatus.ContextMenuOpen; // Löscht das ContextMenuOpen-Flag
            CheckAllConditions();
        }

        public void CheckAllConditions()
        {
            RemoveCrrentPlaceholder();
            // Ausgabe im Klartext
            Console.WriteLine($"Current Dock Status (numeric): {(int)currentDockStatus} - Flags: {currentDockStatus}"); // Debug-Ausgabe im Klartext

            if (currentDockStatus > 0) // Überprüft, ob irgendein Flag gesetzt ist
            {
                Console.WriteLine("Conditions met, showing dock."); // Debug-Ausgabe
                ShowDock();
                categoryHideTimer.Stop();
                dockHideTimer.Stop();
            }
            else
            {
                Console.WriteLine("Conditions not met, hiding dock."); // Debug-Ausgabe
                categoryHideTimer.Start();
                dockHideTimer.Start();
            }
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











        private void DockPanel_PreviewGiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            if (draggedButton != null)
            {
                var mousePosition = Mouse.GetPosition(DockPanel);

                if (mousePosition.X >= 0 && mousePosition.X <= DockPanel.ActualWidth &&
                    mousePosition.Y >= 0 && mousePosition.Y <= DockPanel.ActualHeight)
                {
                    Console.WriteLine($"DockPanel_PreviewGiveFeedback: Dragging: {draggedButton.Tag}, Effects: {e.Effects}, Mouse Position: {mousePosition}"); // Debugging

                    var hitTestResult = VisualTreeHelper.HitTest(DockPanel, mousePosition);
                    if (hitTestResult != null)
                    {
                        var overElement = hitTestResult.VisualHit as UIElement;
                        if (overElement != null)
                        {
                            if (overElement is Button button && button.Tag is DockItem dockItem)
                            {
                                Console.WriteLine($"DockPanel_PreviewGiveFeedback: Über Element: {dockItem.DisplayName}, IsCategory: {dockItem.IsCategory}, Mouse Position: {mousePosition}"); // Debugging
                                if (dockItem.IsCategory)
                                {
                                    // Visuelles Feedback für Kategorie-Elemente
                                    button.Background = new SolidColorBrush(Colors.LightGreen);
                                }
                            }
                            else
                            {
                                Console.WriteLine("DockPanel_PreviewGiveFeedback: Über einem unbekannten Element oder kein DockItem"); // Debugging
                            }
                        }
                        else
                        {
                            Console.WriteLine("DockPanel_PreviewGiveFeedback: Keine Übereinstimmung mit einem Element im DockPanel"); // Debugging
                        }
                    }
                    else
                    {
                        Console.WriteLine("DockPanel_PreviewGiveFeedback: HitTestResult ist null"); // Debugging
                    }
                }
                else
                {
                    Console.WriteLine("DockPanel_PreviewGiveFeedback: Mausposition außerhalb der Grenzen des DockPanels"); // Debugging
                }
            }

            e.UseDefaultCursors = false; // Benutzerdefinierte Cursors verwenden
            Mouse.SetCursor(Cursors.Hand); // Beispiel: Hand-Cursor verwenden, du kannst hier auch dein eigenes Symbol verwenden
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
                    // Console.WriteLine($"Dragging: {draggedButton.Tag}, Position: {position}"); // Debugging
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
                            // Console.WriteLine($"Maus über Element: {button.Tag}, Position: {mousePosition}"); // Debugging
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
                    // Console.WriteLine($"Maus über Dock ohne Element, Position: {mousePosition}"); // Debugging
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

            Console.WriteLine("DockPanel_DragEnter: Aufgerufen"); // Debug-Ausgabe
            currentDockStatus |= DockStatus.DraggingToDock;  // Flag setzen
            CheckAllConditions();

            // Position des Drag-Elements im DockPanel ermitteln
            Point dropPosition = e.GetPosition(DockPanel);
            Console.WriteLine($"DockPanel_DragEnter: Drop-Position: X={dropPosition.X}, Y={dropPosition.Y}"); // Debug-Ausgabe der Position

            // Entferne vorhandene Platzhalter, bevor der neue erstellt wird
            if (currentPlaceholder != null)
            {
                DockPanel.Children.Remove(currentPlaceholder);
                Console.WriteLine("DockPanel_DragEnter: Vorhandenen Platzhalter entfernt.");
            }

            // Verzögerte Erstellung des Platzhalters
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Platzhalter-Element (z.B. transparentes Border)
                currentPlaceholder = new Border
                {
                    Background = new SolidColorBrush(Colors.LightGray),
                    Opacity = 0.0,
                    Height = 0.0, // Höhe des Platzhalters
                    Width = 10, // Breite des Platzhalters
                };

                bool isMouseOnElement = false;

                // Durchlaufe die Kinder des DockPanels, um zu ermitteln, zwischen welchen Elementen das neue Element abgelegt wird
                for (int i = 0; i < DockPanel.Children.Count; i++)
                {
                    // Überprüfen, ob das Kind ein Button ist
                    if (DockPanel.Children[i] is Button button && button.Tag is DockItem dockItem)
                    {
                        // Erhalte die Position jedes vorhandenen Elements
                        Point elementPosition = button.TransformToAncestor(DockPanel).Transform(new Point(0, 0));
                        double elementCenterX = elementPosition.X + (button.RenderSize.Width / 2);
                        Rect elementRect = new Rect(elementPosition, button.RenderSize);

                        Console.WriteLine($"DockPanel_DragEnter: Element {i} - Position: X={elementPosition.X}, Y={elementPosition.Y}, CenterX={elementCenterX}"); // Debug-Ausgabe der Position des Elements

                        // Überprüfen, ob die Maus auf dem aktuellen Element ist
                        if (elementRect.Contains(dropPosition))
                        {
                            Console.WriteLine($"DockPanel_DragEnter: Maus auf Element: DisplayName = {dockItem.DisplayName}, IsCategory = {dockItem.IsCategory}");
                            isMouseOnElement = true;
                            break;
                        }

                        // Überprüfen, ob die Drop-Position vor oder nach dem aktuellen Element ist
                        if (dropPosition.X < elementCenterX)
                        {
                            // Wenn der Platzhalter-Index nicht übereinstimmt, füge den Platzhalter an der neuen Position ein
                            if (currentPlaceholderIndex != i)
                            {
                                currentPlaceholderIndex = i; // Update den Platzhalter-Index
                                DockPanel.Children.Insert(i, currentPlaceholder);
                                Console.WriteLine($"DockPanel_DragEnter: Platzhalter zwischen Element {i - 1} und Element {i} hinzugefügt.");
                            }
                            break; // Hier kannst du weitere Logik hinzufügen, um das Element zu platzieren
                        }
                    }
                    else
                    {
                        Console.WriteLine($"DockPanel_DragEnter: Element {i} ist kein Button, sondern ein {DockPanel.Children[i].GetType().Name}.");
                    }
                }

                if (!isMouseOnElement)
                {
                    Console.WriteLine("DockPanel_DragEnter: Maus zwischen Elementen oder außerhalb von Elementen.");
                }
            }));

            // Prüfen auf Serializable und FileDrop
            if ((e.Data.GetDataPresent(DataFormats.Serializable) || e.Data.GetDataPresent(DataFormats.FileDrop)) && DockPanel != null)
            {
                e.Effects = DragDropEffects.Move;
                DockPanel.Background = new SolidColorBrush(Colors.LightGreen); // Visuelles Feedback
                Console.WriteLine("DockPanel_DragEnter: Element über dem Hauptdock erkannt"); // Debug-Ausgabe
            }
            else
            {
                e.Effects = DragDropEffects.None;
                Console.WriteLine("DockPanel_DragEnter: Kein Element erkannt oder DockPanel ist null"); // Debug-Ausgabe
            }

            // Sicherstellen, dass alle Platzhalter entfernt werden, wenn der Drag-Vorgang beendet ist
            if (currentPlaceholder != null)
            {
                DockPanel?.Children.Remove(currentPlaceholder);
                Console.WriteLine("DockPanel_DragEnter: Platzhalter nach Verlassen der Methode entfernt.");
                currentPlaceholder = null; // Rücksetzen der Referenz
            }
        }



        private void DockPanel_DragLeave(object sender, DragEventArgs e)
        {
            Console.WriteLine("DockPanel_DragLeave: Aufgerufen"); // Debug-Ausgabe
            currentDockStatus &= ~DockStatus.DraggingToDock;  // Flag zurücksetzen, wenn der Drag-Vorgang das DockPanel verlässt
            CheckAllConditions();

            // Entferne den Platzhalter, wenn der Drag-Vorgang das DockPanel verlässt
            if (currentPlaceholder != null)
            {
                DockPanel.Children.Remove(currentPlaceholder);
                Console.WriteLine("DockPanel_DragLeave: Platzhalter entfernt.");
                currentPlaceholder = null; // Setze die Platzhalter-Referenz zurück
                currentPlaceholderIndex = -1; // Zurücksetzen des Platzhalter-Index
            }

            // Visuelles Feedback zurücksetzen
            var brush = (SolidColorBrush)FindResource("PrimaryColor");
            if (brush != null)
            {
                DockPanel.Background = brush; // Setze auf die ursprüngliche Farbe zurück
                Console.WriteLine("DockPanel_DragLeave: Element hat das Hauptdock verlassen und Hintergrund zurückgesetzt"); // Debug-Ausgabe
            }
        }



        // private void DockPanel_Drop(object sender, DragEventArgs e)
        // {
        //     Console.WriteLine("DockPanel_Drop: Aufgerufen"); // Debug-Ausgabe

        //     // Entferne den Platzhalter, wenn das Element abgelegt wird
        //     if (currentPlaceholder != null)
        //     {
        //         DockPanel.Children.Remove(currentPlaceholder);
        //         Console.WriteLine("DockPanel_Drop: Platzhalter entfernt.");
        //         currentPlaceholder = null; // Setze die Platzhalter-Referenz zurück
        //         currentPlaceholderIndex = -1; // Zurücksetzen des Platzhalter-Index
        //     }

        //     // Weitere Logik für das Ablegen des Elements hier einfügen
        //     // Zum Beispiel: das tatsächliche Hinzufügen des Drag-Elements an die Position des Platzhalters
        // }

        // original Methode
        // private void DockPanel_DragLeave(object sender, DragEventArgs e)
        // {
        //     Console.WriteLine("DockPanel_DragLeave: Aufgerufen"); // Debug-Ausgabe
        //     currentDockStatus &= ~DockStatus.DraggingToDock;
        //     CheckAllConditions();

        //     // Visuelles Feedback zurücksetzen
        //     var brush = (SolidColorBrush)FindResource("PrimaryColor");
        //     if (brush != null)
        //     {
        //         DockPanel.Background = brush; // Setze auf die ursprüngliche Farbe zurück
        //         Console.WriteLine("DockPanel_DragLeave: Element hat das Hauptdock verlassen und Hintergrund zurückgesetzt"); // Debug-Ausgabe
        //     }
        // }





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
                            if (!isCategoryMessageShown)
                            {
                                // MessageBox.Show("Kategorie-Elemente können nicht in das Kategorie-Dock verschoben werden.", "Verschieben nicht erlaubt", MessageBoxButton.OK, MessageBoxImage.Information);
                                isCategoryMessageShown = true; // Nachricht wurde gezeigt
                            }
                           
                            return; // Abbrechen, wenn es eine Kategorie ist
                        }

                        // Überprüfen, ob das Element bereits einer anderen Kategorie zugewiesen ist
                        if (string.IsNullOrEmpty(droppedItem.Category) || droppedItem.Category == currentOpenCategory)
                        {
                            // Entferne das Element nicht, wenn es vom Hauptdock kommt und keine Kategorie hat
                            if (!string.IsNullOrEmpty(droppedItem.Category))
                            {
                                var parent = VisualTreeHelper.GetParent(button) as Panel;
                                if (parent != null)
                                {
                                    parent.Children.Remove(button);
                                }
                            }

                            droppedItem.Category = currentOpenCategory;

                            // Logische Trennung durchführen, bevor das Element hinzugefügt wird
                            if (button.Parent != null)
                            {
                                var logicalParent = LogicalTreeHelper.GetParent(button) as Panel;
                                if (logicalParent != null)
                                {
                                    logicalParent.Children.Remove(button);
                                }
                            }

                            // Position innerhalb des Kategorie-Docks bestimmen
                            Point dropPosition = e.GetPosition(CategoryDockContainer);
                            double dropCenterX = dropPosition.X;
                            int newIndex = 0;
                            bool inserted = false;
                            for (int i = 0; i < CategoryDockContainer.Children.Count; i++)
                            {
                                if (CategoryDockContainer.Children[i] is Button existingButton)
                                {
                                    Point elementPosition = existingButton.TranslatePoint(new Point(0, 0), CategoryDockContainer);
                                    double elementCenterX = elementPosition.X + (existingButton.ActualWidth / 2);
                                    if (dropCenterX < elementCenterX)
                                    {
                                        CategoryDockContainer.Children.Insert(i, button);
                                        inserted = true;
                                        break;
                                    }
                                }
                                newIndex++;
                            }
                            if (!inserted)
                            {
                                CategoryDockContainer.Children.Add(button);
                            }

                            CategoryDockContainer.Background = new SolidColorBrush(Colors.Transparent); // Visuelles Feedback zurücksetzen

                            // Aktualisiere die interne Struktur oder Daten, falls nötig
                            UpdateDockItemLocation(button);

                            // Dock-Items speichern
                            dockManager.SaveDockItems(currentOpenCategory); // Verwende die gespeicherte Kategorie
                        }
                    }
                }
            }
            else
            {
                e.Handled = true; // Stelle sicher, dass das Ereignis verarbeitet wurde
            }

            // Visuelles Feedback zurücksetzen
            CategoryDockContainer.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")); // Sicherstellen, dass das Kategorie-Dock korrekt zurückgesetzt wird
            CheckAllConditions();
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
            CheckAllConditions();
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
            Console.WriteLine($"OpenDockItem aufgerufen"); // Debug-Ausgabe
            if (!string.IsNullOrEmpty(dockItem.FilePath))
            {
                Console.WriteLine($"OpenDockItem aufgerufen, filePath: {dockItem.FilePath}"); // Debug-Ausgabe
                OpenFile(dockItem.FilePath);
            }
            else
            {
                Console.WriteLine("OpenDockItem aufgerufen, Kategorie"); // Debug-Ausgabe
                if (isCategoryDockOpen && currentOpenCategory == dockItem.DisplayName)
                {
                    // Kategoriedock schließen
                    CategoryDockContainer.Visibility = Visibility.Collapsed;
                    CategoryDockBorder.Visibility = Visibility.Collapsed;
                    OverlayCanvas.Visibility = Visibility.Collapsed;
                    currentDockStatus &= ~DockStatus.CategoryElementClicked; // Flag zurücksetzen

                    isCategoryDockOpen = false;
                    Console.WriteLine($"OpenDockItem Kategoriedock {dockItem.DisplayName} ID: {dockItem.Id} geschlossen"); // Debug-Ausgabe
                    isCategoryDockOpenID = "";
                }
                else
                {
                    // Kategoriedock öffnen
                    ShowCategoryDockPanel(new StackPanel
                    {
                        Name = dockItem.DisplayName,
                        // Children = { new Button { Content = $"Kategorie: {dockItem.DisplayName}", Width = 100, Height = 50 } }
                    });
                    currentDockStatus |= DockStatus.CategoryElementClicked; // Flag setzen
                    isCategoryDockOpen = true;
                    Console.WriteLine($"OpenDockItem Kategoriedock {dockItem.DisplayName} ID: {dockItem.Id} geöffnet"); // Debug-Ausgabe
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
            Console.WriteLine("OpenFile: Dock schließen");
            HideCategoryDockPanel();
            HideDock();
            currentDockStatus = DockStatus.None;
            // CheckAllConditions();


        }



        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (DockContextMenu.PlacementTarget is Button button && button.Tag is DockItem dockItem)
            {
                var customMessageBox = new CustomMessageBox($"Möchtest du das Element '{dockItem.DisplayName}' wirklich löschen?");
                customMessageBox.ShowDialog();

                if (customMessageBox.Result)
                {
                    // Hier ermitteln wir die Kategorie, die gelöscht werden soll
                    string currentCategory = dockItem.Category;

                    // Übergabe der Kategorie an RemoveDockItem
                    dockManager.RemoveDockItem(button, currentCategory);
                }
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
    // Setzt die grundlegenden Eigenschaften und fügt das CategoryDock hinzu
    CategoryDockContainer.Children.Clear();
    CategoryDockContainer.Children.Add(categoryDock);
    CategoryDockContainer.Visibility = Visibility.Visible;
    CategoryDockBorder.Visibility = Visibility.Visible;
    // OverlayCanvas.Visibility = Visibility.Visible;

    // Verwende Dispatcher, um den Margin-Wert nach dem Rendern zu setzen
    Application.Current.Dispatcher.InvokeAsync(() =>
    {
        if (CategoryDockBorder.ActualWidth > 0)
        {
            CategoryDockBorder.Margin = new Thickness(
                dockManager.mousePositionSave - (CategoryDockBorder.ActualWidth / 2) ,
                0, 0, 0
            );
            Canvas.SetLeft(OverlayCanvasHorizontalLine, dockManager.mousePositionSave );
            Console.WriteLine($"Nach dem Rendern CategoryDockBorder: {CategoryDockBorder.ActualWidth}"); // Debugging
        }
    }, System.Windows.Threading.DispatcherPriority.Loaded);

    // Restlicher Code
    currentDockStatus |= DockStatus.CategoryElementClicked;
    currentOpenCategory = categoryDock.Name;
    var items = SettingsManager.LoadSettings();
    foreach (var item in items)
    {
        if (!string.IsNullOrEmpty(item.Category) && item.Category == currentOpenCategory)
        {
            dockManager.AddDockItemAt(item, CategoryDockContainer.Children.Count, currentOpenCategory);
        }
    }

    MainStackPanel.Margin = new Thickness(0);
    categoryHideTimer.Start();
    CheckAllConditions();
}



        public void HideCategoryDockPanel()
        {
            // Console.WriteLine("HideCategoryDockPanel aufgerufen"); // Debugging
            CategoryDockContainer.Visibility = Visibility.Collapsed;
            CategoryDockBorder.Visibility = Visibility.Collapsed; // Sichtbarkeit der CategoryDockBorder ändern
            OverlayCanvas.Visibility = Visibility.Collapsed;

            // MainStackPanel zurücksetzen
            MainStackPanel.Margin = new Thickness(0, 0, 0, 0);

            // Timer stoppen
            categoryHideTimer.Stop();
            Console.WriteLine("CategoryDockContainer ausgeblendet, MainStackPanel neu positioniert."); // Debugging
            // CheckAllConditions();
             isCategoryMessageShown = false; // Nachricht-Flag sofort zurücksetzen
        }



        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DockContextMenu.PlacementTarget is Button button && button.Tag is DockItem dockItem)
                {
                    // Alle Dock-Items laden
                    var dockItems = SettingsManager.LoadSettings() ?? new List<DockItem>();

                    // Prüfen, ob das aktuelle Item eine Kategorie ist
                    var settings = dockItems.FirstOrDefault(di => di.Id == dockItem.Id);
                    if (settings == null)
                    {
                        MessageBox.Show("Fehler beim Laden der Dock-Einstellungen.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Edit-Dialog initialisieren
                    EditPropertiesWindow editWindow = new EditPropertiesWindow
                    {
                        Owner = this,
                        NameTextBox = { Text = settings.DisplayName },
                        IconSourceTextBox = { Text = settings.IconSource } // Hinzufügen des Bildpfads
                    };

                    bool? dialogResult = editWindow.ShowDialog();
                    if (dialogResult == true)
                    {
                        string newName = editWindow.NameTextBox.Text.Trim();
                        string newIconPath = editWindow.IconSourceTextBox.Text.Trim(); // Neues Bildpfad

                        // Neuen Namen validieren
                        if (string.IsNullOrEmpty(newName))
                        {
                            MessageBox.Show("Name darf nicht leer sein.", "Ungültiger Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Prüfen, ob der neue Name bereits für eine andere Kategorie verwendet wird
                        if (dockItems.Any(di => di.IsCategory && di.DisplayName == newName && di.Id != settings.Id)) // Sicherstellen, dass es nicht dasselbe ist
                        {
                            MessageBox.Show("Eine Kategorie mit diesem Namen existiert bereits.", "Ungültiger Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Prüfen, ob der Kategoriename geändert wurde
                        bool nameChanged = settings.DisplayName != newName;

                        // Falls es eine Kategorie ist
                        if (settings.IsCategory)
                        {
                            if (nameChanged)
                            {
                                string oldCategory = settings.DisplayName;
                                settings.DisplayName = newName;

                                // Aktualisiere alle untergeordneten Elemente, die zur alten Kategorie gehören
                                foreach (var item in dockItems)
                                {
                                    if (!item.IsCategory && item.Category == oldCategory)
                                    {
                                        item.Category = newName;
                                    }
                                }

                                // Aktualisiere den Button-Text
                                var textBlock = new TextBlock
                                {
                                    Text = newName,
                                    TextAlignment = TextAlignment.Center,
                                    TextWrapping = TextWrapping.Wrap,
                                    Width = 60,
                                    Margin = new Thickness(5)
                                };

                                var stackPanel = button.Content as StackPanel;
                                if (stackPanel != null)
                                {
                                    stackPanel.Children.RemoveAt(1);
                                    stackPanel.Children.Add(textBlock);
                                }
                            }

                            // Wenn der Name nicht geändert wurde, nur das Symbol aktualisieren
                            if (!string.IsNullOrEmpty(newIconPath))
                            {
                                settings.IconSource = newIconPath; // Hier den Bildpfad aktualisieren
                            }

                            button.Tag = dockItem;

                            // Speichern der aktualisierten Einstellungen
                            SettingsManager.SaveSettings(dockItems);
                            dockManager.LoadDockItems();
                        }
                        else
                        {
                            MessageBox.Show("Dieses Element ist keine Kategorie.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Fehler: DockContextMenu.PlacementTarget ist kein Button oder button.Tag ist kein DockItem", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private SettingsWindow settingsWindow = new SettingsWindow();

        private void CheckAutostart()
        {
            AutostartCheckBox.IsChecked = StartupManager.IsInStartup();
        }

        private void AutostartCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            StartupManager.AddToStartup(true);
        }

        private void AutostartCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            StartupManager.AddToStartup(false);
        }

        private void RemoveCrrentPlaceholder()
        {
            // Entferne vorhandene Platzhalter, bevor der neue erstellt wird
            if (currentPlaceholder != null)
            {
                DockPanel.Children.Remove(currentPlaceholder);
                Console.WriteLine("DockPanel_DragEnter: Vorhandenen Platzhalter entfernt.");
            }
        }


        protected override void OnClosed(EventArgs e)
        {
            // mouseHook.Stop();
            // base.OnClosed(e);
        }








    }

}