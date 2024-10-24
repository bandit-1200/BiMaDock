using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MyDockApp
{
    public partial class MainWindow : Window
    {
        private DockManager dockManager;
        // private bool isDragging = false;
        private bool dockVisible = false;
        public bool IsDragging => isDragging; 
        private Point? dragStartPoint = null;
        private Button? draggedButton = null;
        private bool isDragging = false; // Flag für Dragging
             public void SetDragging(bool value)
    {
        isDragging = value;
    }

public MainWindow()
{
    InitializeComponent();
    AllowDrop = true;
    dockManager = new DockManager(DockPanel, this);
    dockManager.LoadDockItems();
    this.Closing += (s, e) => dockManager.SaveDockItems();
    DockPanel.PreviewMouseLeftButtonDown += DockPanel_MouseLeftButtonDown;
    DockPanel.PreviewMouseMove += DockPanel_MouseMove;
    DockPanel.PreviewMouseLeftButtonUp += DockPanel_MouseLeftButtonUp;
    DockPanel.Drop += dockManager.DockPanel_Drop; // Drop-Ereignis für das Dock-Panel hinzufügen

    this.Loaded += (s, e) =>
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        this.Left = (screenWidth / 2) - (this.Width / 2);
        this.Top = -this.Height + 5; // Fenster fast unsichtbar positionieren

        // Mausbewegungs-Event hinzufügen
        this.MouseMove += CheckMousePosition; 
        DockPanel.MouseEnter += DockPanel_MouseEnter;
        DockPanel.MouseLeave += DockPanel_MouseLeave;
        DockPanel.DragEnter += (s, e) =>
        {
            e.Effects = DragDropEffects.All;
            if (!dockVisible) ShowDock();
        }; // DragEnter-Event hinzufügen
    };

    DockPanel.MouseRightButtonDown += (s, e) =>
    {
        OpenMenuItem.Visibility = Visibility.Collapsed;
        DeleteMenuItem.Visibility = Visibility.Collapsed;
        EditMenuItem.Visibility = Visibility.Collapsed;
        DockContextMenu.IsOpen = true;
    };
}

private void CheckMousePosition(object sender, MouseEventArgs e)
{
    var mousePos = Mouse.GetPosition(this);
    var screenPos = PointToScreen(mousePos);
    Console.WriteLine($"Mouse Pos: X={screenPos.X}, Y={screenPos.Y}, Dragging: {isDragging}"); // Debug-Ausgabe der Mausposition

    if (screenPos.Y <= 5 && !dockVisible)
    {
        Console.WriteLine("Condition met: ShowDock"); // Debug-Ausgabe
        ShowDock();
    }
    else if (screenPos.Y > this.Height + 150 && dockVisible && !isDragging) // Großzügigerer Abstand für das Ausblenden
    {
        Console.WriteLine("Condition met: HideDock"); // Debug-Ausgabe
        HideDock();
    }
    else
    {
        Console.WriteLine("No condition met"); // Debug-Ausgabe
        Console.WriteLine($"Dock Height: {this.Height}, ScreenPos.Y: {screenPos.Y}, Dragging: {isDragging}"); // Debug-Ausgabe
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
            From = -this.Height + 5,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(500))
        };
        this.BeginAnimation(Window.TopProperty, slideAnimation);
    }
}

public void HideDock()
{
    if (dockVisible && !isDragging)
    {
        Console.WriteLine("HideDock aufgerufen");
        dockVisible = false;
        var slideAnimation = new DoubleAnimation
        {
            From = 0,
            To = -this.Height + 5,
            Duration = new Duration(TimeSpan.FromMilliseconds(500))
        };
        this.BeginAnimation(Window.TopProperty, slideAnimation);
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
    isDragging = false; // Dragging-Flag zurücksetzen
    Console.WriteLine("Drag End"); // Debugging

    // Dock wieder ausblenden, falls die Maus außerhalb ist
    var mousePos = Mouse.GetPosition(this);
    var screenPos = PointToScreen(mousePos);
    if (screenPos.Y > this.Height + 10)
    {
        HideDock();
    }

    // Überprüfen, ob ein Element geöffnet werden soll
    if (e.OriginalSource is FrameworkElement originalSource)
    {
        while (originalSource != null && !(originalSource is Button))
        {
            originalSource = originalSource.Parent as FrameworkElement;
        }
        if (originalSource is Button button && button.Tag is string filePath)
        {
            Console.WriteLine("Element ausgewählt: " + filePath); // Debugging
            OpenFile(filePath);
        }
        else
        {
            Console.WriteLine("Kein gültiger Button oder Dateipfad"); // Debugging
        }
    }
    else
    {
        Console.WriteLine("Kein gültiges FrameworkElement"); // Debugging
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
    if (!isDragging)
    {
        HideDock();
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
            if (DockContextMenu.PlacementTarget is Button button && button.Tag is string filePath)
            {
                Console.WriteLine("Löschen des Elements: " + filePath); // Debug-Ausgabe
                dockManager.RemoveDockItem(button);
            }
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

