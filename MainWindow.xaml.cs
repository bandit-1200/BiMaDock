using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MyDockApp
{
    public partial class MainWindow : Window
    {
        private DockManager dockManager;
        // private Point? dragStartPoint = null;
        // private Button? draggedButton = null;
        private Point? dragStartPoint = null;
        private Button? draggedButton = null;


        public MainWindow()
        {
            InitializeComponent();
            AllowDrop = true;
            dockManager = new DockManager(DockPanel, this); // Aktualisieren, um das Hauptfenster zu übergeben
            dockManager.LoadDockItems();
            this.Closing += (s, e) => dockManager.SaveDockItems(); // Einstellungen beim Schließen speichern

            DockPanel.Drop += DockPanel_Drop;

            DockPanel.PreviewMouseLeftButtonDown += DockPanel_MouseLeftButtonDown;
            DockPanel.PreviewMouseMove += DockPanel_MouseMove;
            DockPanel.PreviewMouseLeftButtonUp += DockPanel_MouseLeftButtonUp;



            // Kontextmenü anzeigen beim Rechtsklick auf das DockPanel
            DockPanel.MouseRightButtonDown += (s, e) =>
            {
                OpenMenuItem.Visibility = Visibility.Collapsed;
                DeleteMenuItem.Visibility = Visibility.Collapsed;
                EditMenuItem.Visibility = Visibility.Collapsed;
                DockContextMenu.IsOpen = true;
            };
        }

private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
{
    Console.WriteLine("Mouse Down Event ausgelöst"); // Debugging
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
    Console.WriteLine("Mouse Move Event ausgelöst"); // Debugging
    if (dragStartPoint.HasValue)
    {
        Console.WriteLine("dragStartPoint: " + dragStartPoint.Value); // Debugging
    }
    if (draggedButton != null)
    {
        Console.WriteLine("draggedButton: " + draggedButton.Tag); // Debugging
    }
    if (e.LeftButton == MouseButtonState.Pressed && draggedButton != null && dragStartPoint.HasValue)
    {
        Point position = e.GetPosition(DockPanel);
        Vector diff = dragStartPoint.Value - position;

        Console.WriteLine("Diff X: " + diff.X + ", Diff Y: " + diff.Y); // Debugging
        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            Console.WriteLine("Dragging: " + draggedButton.Tag); // Debugging
            DragDrop.DoDragDrop(draggedButton, new DataObject(DataFormats.Serializable, draggedButton), DragDropEffects.Move);
            dragStartPoint = null;
            draggedButton = null;
        }
    }
}








        private void DockPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = null;
            draggedButton = null;
            Console.WriteLine("Drag End"); // Debugging
        }



private void DockPanel_Drop(object sender, DragEventArgs e)
{
    if (e.Data.GetDataPresent(DataFormats.Serializable))
    {
        Button? droppedButton = e.Data.GetData(DataFormats.Serializable) as Button;
        if (droppedButton != null)
        {
            Console.WriteLine("Drop: " + droppedButton.Tag); // Debugging

            // Entferne das Element aus seiner aktuellen Position
            DockPanel.Children.Remove(droppedButton);

            // Bestimme die neue Position
            Point dropPosition = e.GetPosition(DockPanel);
            int index = 0;

            foreach (UIElement element in DockPanel.Children)
            {
                if (element is Button button && dropPosition.X < button.TranslatePoint(new Point(0, 0), DockPanel).X)
                {
                    break;
                }
                index++;
            }

            // Füge das Element an der neuen Position ein
            DockPanel.Children.Insert(index, droppedButton);
            dockManager.SaveDockItems();
            Console.WriteLine("Drop an Position: " + index); // Debugging
        }
        else
        {
            Console.WriteLine("Kein gültiger Button gedroppt"); // Debugging
        }
    }
    else
    {
        Console.WriteLine("Kein gültiges Drop-Format erkannt"); // Debugging
    }
}












        // Event-Handler für das Beenden des Docks
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Platzhalter-Event-Handler für elementspezifische Aktionen
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

        // Methode zum Öffnen der Datei
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

        // Platzhalter für weitere Methoden
        // Event-Handler für das Löschen eines Dock-Elements
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (DockContextMenu.PlacementTarget is Button button && button.Tag is string filePath)
            {
                Console.WriteLine("Löschen des Elements: " + filePath); // Debug-Ausgabe
                dockManager.RemoveDockItem(button);
            }
        }


        // Event-Handler für das Bearbeiten der Eigenschaften eines Dock-Elements
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (DockContextMenu.PlacementTarget is Button button && button.Tag is string filePath)
            {
                // Fenster zum Bearbeiten der Eigenschaften öffnen
                EditPropertiesWindow editWindow = new EditPropertiesWindow
                {
                    Owner = this,
                    NameTextBox = { Text = System.IO.Path.GetFileNameWithoutExtension(filePath) },
                    PathTextBox = { Text = filePath }
                };

                if (editWindow.ShowDialog() == true)
                {
                    // Änderungen übernehmen und Dock-Element aktualisieren
                    string newName = editWindow.NameTextBox.Text;
                    string newPath = editWindow.PathTextBox.Text;

                    if (!string.IsNullOrEmpty(newName) && !string.IsNullOrEmpty(newPath))
                    {
                        // Symbol beibehalten
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

                        button.Content = stackPanel; // Aktualisieren des Inhalts des Buttons
                        button.Tag = newPath; // Pfad im Tag des Buttons aktualisieren
                        dockManager.SaveDockItems(); // Änderungen speichern
                    }
                }
            }
        }



    }
}
