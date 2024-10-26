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
        public bool dockVisible = false;
        public bool IsDragging => isDragging;
        private Point? dragStartPoint = null;
        private Button? draggedButton = null;
        public bool isDragging = false; // Flag für Dragging
        private DispatcherTimer dockHideTimer;
        private DispatcherTimer categoryHideTimer;

        public void SetDragging(bool value)
        {
            isDragging = value;
        }

public MainWindow()
{
    InitializeComponent();
    AllowDrop = true;
    Console.WriteLine("Hauptfenster initialisiert."); // Debugging

    dockManager = new DockManager(DockPanel, this);
    dockManager.LoadDockItems();
    Console.WriteLine("Dock-Elemente geladen."); // Debugging

    // Timer initialisieren
    dockHideTimer = new DispatcherTimer();
    dockHideTimer.Interval = TimeSpan.FromSeconds(1); // Zeitintervall auf eine Sekunde setzen
    dockHideTimer.Tick += (s, e) =>
    {
        if (dockVisible)
        {
            HideDock();
        }
    };

    // Timer initialisieren
    categoryHideTimer = new DispatcherTimer();
    categoryHideTimer.Interval = TimeSpan.FromSeconds(1); // Zeitintervall auf eine Sekunde setzen
    categoryHideTimer.Tick += (s, e) =>
    {
        HideCategoryDockPanel();
    };

    this.Closing += (s, e) => dockManager.SaveDockItems();
    DockPanel.PreviewMouseLeftButtonDown += DockPanel_MouseLeftButtonDown;
    DockPanel.PreviewMouseMove += DockPanel_MouseMove;
    DockPanel.PreviewMouseLeftButtonUp += DockPanel_MouseLeftButtonUp;
    this.Loaded += (s, e) =>
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        this.Left = (screenWidth / 2) - (this.Width / 2);
        this.Top = 0; // Fenster am oberen Bildschirmrand positionieren

        this.MouseMove += CheckMousePosition;
        DockPanel.MouseEnter += DockPanel_MouseEnter;
        DockPanel.MouseLeave += DockPanel_MouseLeave;
        DockPanel.DragEnter += (s, e) =>
        {
            e.Effects = DragDropEffects.All;
            if (!dockVisible) ShowDock();
        };
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
    Console.WriteLine("Event-Handler zugewiesen."); // Debugging
}

 
private void CheckMousePosition(object sender, MouseEventArgs e)
{
    if (DockPanel != null && e != null)  // Sicherstellen, dass DockPanel und e nicht null sind
    {
        var mousePos = Mouse.GetPosition(DockPanel);
        var screenPos = PointToScreen(mousePos);
        Console.WriteLine($"Mouse Pos: X={screenPos.X}, Y={screenPos.Y}, Dragging: {isDragging}, ContextMenu Open: {DockContextMenu?.IsOpen}"); // Debug-Ausgabe der Mausposition und Kontextmenüstatus

        if (screenPos.Y <= DockPanel.ActualHeight && !dockVisible)  // Überprüfen, ob die Maus innerhalb der Dockhöhe ist
        {
            Console.WriteLine("Condition met: ShowDock");  // Debug-Ausgabe
            ShowDock();
            dockHideTimer.Stop();  // Timer stoppen, wenn das Dock eingeblendet wird
        }
        else if (screenPos.Y > DockPanel.ActualHeight && dockVisible && !isDragging && !(DockContextMenu?.IsOpen ?? false))  // Kontextmenü-Bedingung hinzufügen
        {
            Console.WriteLine("Condition met: HideDock");  // Debug-Ausgabe
            HideDock();
        }
        else
        {
            Console.WriteLine("No condition met");  // Debug-Ausgabe
            Console.WriteLine($"Dock Height: {this.Height}, MousePos.Y: {mousePos.Y}, Dragging: {isDragging}, ContextMenu Open: {DockContextMenu?.IsOpen}");  // Debug-Ausgabe

            if (!(DockContextMenu?.IsOpen ?? false))
            {
                dockHideTimer.Stop();  // Timer stoppen, wenn die Maus über dem Dock ist und Kontextmenü nicht geöffnet ist
                dockHideTimer.Start();  // Timer neu starten
            }
        }
    }
    else
    {
        Console.WriteLine("Fehler: DockPanel oder EventArgs sind null.");  // Debug-Ausgabe
    }
}


public void ShowDock()
{
    if (!dockVisible)
    {
        Console.WriteLine("ShowDock aufgerufen");
        dockVisible = true;
        var slideAnimation = new DoubleAnimation
        {
            From = -DockPanel.ActualHeight + 5,  // Startposition der Animation (teilweise sichtbar)
            To = 0,  // Endposition der Animation (sichtbare Position)
            Duration = new Duration(TimeSpan.FromMilliseconds(500)),
            FillBehavior = FillBehavior.Stop
        };
        slideAnimation.Completed += (s, e) => 
        {
            DockPanel.Margin = new Thickness(0, 0, 0, 0);
            Console.WriteLine("Dock vollständig sichtbar"); // Debugging
        };
        DockPanel.BeginAnimation(Canvas.TopProperty, slideAnimation);
    }
    else
    {
        Console.WriteLine("Dock ist bereits sichtbar, keine Animation"); // Debugging
    }
}














public void HideDock()
{
    if (dockVisible)
    {
        Console.WriteLine("HideDock aufgerufen");
        dockVisible = false;
        var slideAnimation = new DoubleAnimation
        {
            From = 0,  // Startposition der Animation (sichtbar)
            To = -DockPanel.ActualHeight + 5,  // Endposition der Animation (5 Pixel sichtbar lassen)
            Duration = new Duration(TimeSpan.FromMilliseconds(500)),
            FillBehavior = FillBehavior.Stop
        };
        slideAnimation.Completed += (s, e) => 
        {
            DockPanel.Margin = new Thickness(0, -DockPanel.ActualHeight + 5, 0, 0);
            Console.WriteLine("Dock teilweise ausgeblendet, 5 Pixel sichtbar"); // Debugging
        };
        DockPanel.BeginAnimation(Canvas.TopProperty, slideAnimation);
    }
    else
    {
        Console.WriteLine("Dock ist bereits unsichtbar, keine Animation"); // Debugging
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
                }
            }
            else
            {
                Point mousePosition = e.GetPosition(DockPanel);
                bool isOverElement = false;
                UIElement? previousElement = null;
                UIElement? nextElement = null;
                for (int i = 0; i < DockPanel.Children.Count; i++)
                {
                    if (DockPanel.Children[i] is Button button)
                    {
                        Rect elementRect = new Rect(button.TranslatePoint(new Point(0, 0), DockPanel), button.RenderSize);
                        if (elementRect.Contains(mousePosition))
                        {
                            Console.WriteLine($"Maus über Element: {button.Tag}, Position: {mousePosition}"); // Debugging
                            isOverElement = true;
                            break;
                        }
                        else if (mousePosition.X < elementRect.Left)
                        {
                            previousElement = (i > 0) ? DockPanel.Children[i - 1] : null;
                            nextElement = DockPanel.Children[i];
                            break;
                        }
                    }
                }
                if (!isOverElement)
                {
                    if (previousElement is Button prevButton && nextElement is Button nextButton)
                    {
                        Console.WriteLine($"Maus zwischen Elementen: {prevButton.Tag} und {nextButton.Tag}, Position: {mousePosition}"); // Debugging
                    }
                    else if (nextElement is Button onlyNextButton)
                    {
                        Console.WriteLine($"Maus vor dem ersten Element: {onlyNextButton.Tag}, Position: {mousePosition}"); // Debugging
                    }
                    else if (previousElement is Button onlyPrevButton)
                    {
                        Console.WriteLine($"Maus nach dem letzten Element: {onlyPrevButton.Tag}, Position: {mousePosition}"); // Debugging
                    }
                    else
                    {
                        Console.WriteLine($"Maus über Dock ohne Element, Position: {mousePosition}"); // Debugging
                    }
                }

                // Timer zurücksetzen und prüfen, ob das Kontextmenü geöffnet ist
                if (!DockContextMenu.IsOpen)
                {
                    dockHideTimer.Stop();
                    dockHideTimer.Start();
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




        private void DockPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!isDragging)
            {
                ShowDock();
            }
        }

        private void DockPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!isDragging && !DockContextMenu.IsOpen)
            {
                // HideDock();
            }
        }



        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if (DockContextMenu.PlacementTarget is Button button)
            {
                Console.WriteLine("Open_Click aufgerufen. Button Tag: " + (button.Tag ?? "null")); // Debug-Ausgabe des Tags
                if (button.Tag is string filePath)
                {
                    OpenFile(filePath);
                }
                else
                {
                    Console.WriteLine("Tag ist kein Dateipfad"); // Debug-Ausgabe bei Fehler
                }
            }
            else
            {
                Console.WriteLine("PlacementTarget ist kein Button"); // Debug-Ausgabe bei Fehler
            }
        }




        public void OpenFile(string filePath)
        {
            try
            {
                Console.WriteLine("Dateipfad: " + filePath); // Ausgabe des Dateipfads
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Öffnen der Datei: " + ex.Message); // Debug-Ausgabe bei Fehler
            }
        }


        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (DockContextMenu.PlacementTarget is Button button && button.Tag is DockItem dockItem)
            {
                if (button != null && dockItem != null && dockManager != null)
                {
                    Console.WriteLine("Löschen des Elements: " + dockItem.FilePath); // Debug-Ausgabe
                    Console.WriteLine("Button gefunden: " + button.Name);
                    Console.WriteLine("Dock-Manager Status: " + (dockManager != null ? "Existiert" : "Fehlt"));

                    dockManager.RemoveDockItem(button);
                }
                else
                {
                    if (button == null)
                        Console.WriteLine("Fehler: button ist null.");
                    if (dockItem == null)
                        Console.WriteLine("Fehler: dockItem ist null.");
                    if (dockManager == null)
                        Console.WriteLine("Fehler: dockManager ist null.");
                }
            }
            else
            {
                // Debug-Ausgabe, wenn die Bedingung nicht erfüllt ist
                Console.WriteLine("Fehler: DockContextMenu.PlacementTarget ist kein Button oder button.Tag ist kein DockItem.");
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


        private void Kategorie_Click(object sender, RoutedEventArgs e)
        {
            ShowCategoryDockPanel(new StackPanel
            {
                Children = { new Button { Content = "Kategorie-Element", Width = 100, Height = 50 } }
            });
        }

        public void ShowCategoryDockPanel(StackPanel categoryDock)
        {
            Console.WriteLine("ShowCategoryDockPanel aufgerufen"); // Debugging

            CategoryDockContainer.Children.Clear(); // Existierende Dockbar leeren
            CategoryDockContainer.Children.Add(categoryDock); // Neue Dockbar hinzufügen
            CategoryDockContainer.Visibility = Visibility.Visible; // Sichtbarkeit der zweiten Dockbar setzen

            // MainStackPanel nach unten verschieben, um Platz zu schaffen
            MainStackPanel.Margin = new Thickness(0, 0, 0, categoryDock.ActualHeight);

            // Timer starten
            categoryHideTimer.Start();

            Console.WriteLine("CategoryDockContainer ist jetzt sichtbar, MainStackPanel neu positioniert."); // Debugging
        }




        public void HideCategoryDockPanel()
        {
            Console.WriteLine("HideCategoryDockPanel aufgerufen"); // Debugging

            CategoryDockContainer.Children.Clear();
            CategoryDockContainer.Visibility = Visibility.Collapsed;

            // MainStackPanel zurücksetzen
            MainStackPanel.Margin = new Thickness(0, 0, 0, 0);

            // Timer stoppen
            categoryHideTimer.Stop();

            Console.WriteLine("CategoryDockContainer ausgeblendet, MainStackPanel neu positioniert."); // Debugging
        }







        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (DockContextMenu.PlacementTarget is Button button && button.Tag is string filePath)
            {
                EditPropertiesWindow editWindow = new EditPropertiesWindow
                {
                    Owner = this,
                    NameTextBox = { Text = System.IO.Path.GetFileNameWithoutExtension(filePath) },
                    PathTextBox = { Text = filePath }
                };
                if (editWindow.ShowDialog() == true)
                {
                    string newName = editWindow.NameTextBox.Text;
                    string newPath = editWindow.PathTextBox.Text;
                    if (!string.IsNullOrEmpty(newName) && !string.IsNullOrEmpty(newPath))
                    {
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
                        button.Tag = newPath;
                        dockManager.SaveDockItems();
                    }
                }
            }
        }

    }

}