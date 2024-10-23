using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using MyDockApp;
using System.Windows.Input;


public class DockManager
{
    private StackPanel dockPanel;
    private MainWindow mainWindow;
    private Point? dragStartPoint = null;  // Definition hinzugefügt
    private Button? draggedButton = null;  // Definition hinzugefügt




    public DockManager(StackPanel panel, MainWindow window)
    {
        dockPanel = panel;
        mainWindow = window;
        dockPanel.Drop += DockPanel_Drop;
        dockPanel.MouseMove += DockPanel_MouseMove;  // Event-Handler für MouseMove hinzufügen
    }

private void DockPanel_MouseMove(object sender, MouseEventArgs e)
{
    Point mousePosition = e.GetPosition(dockPanel);
    bool isOverElement = false;
    UIElement? previousElement = null;
    UIElement? nextElement = null;

    for (int i = 0; i < dockPanel.Children.Count; i++)
    {
        if (dockPanel.Children[i] is Button button)
        {
            Rect elementRect = new Rect(button.TranslatePoint(new Point(0, 0), dockPanel), button.RenderSize);
            if (elementRect.Contains(mousePosition))
            {
                Console.WriteLine($"Maus über Element: {button.Tag}");
                isOverElement = true;
                break;
            }
            else if (mousePosition.X < elementRect.Left)
            {
                previousElement = (i > 0) ? dockPanel.Children[i - 1] : null;
                nextElement = dockPanel.Children[i];
                break;
            }
        }
    }

    if (!isOverElement)
    {
        if (previousElement is Button prevButton && nextElement is Button nextButton)
        {
            Console.WriteLine($"Maus zwischen Elementen: {prevButton.Tag} und {nextButton.Tag}");
        }
        else if (nextElement is Button onlyNextButton)
        {
            Console.WriteLine($"Maus vor dem ersten Element: {onlyNextButton.Tag}");
        }
        else if (previousElement is Button onlyPrevButton)
        {
            Console.WriteLine($"Maus nach dem letzten Element: {onlyPrevButton.Tag}");
        }
        else
        {
            Console.WriteLine("Maus über Dock ohne Element");
        }
    }
}



    public void LoadDockItems()
    {
        var items = SettingsManager.LoadSettings();
        foreach (var item in items)
        {
            AddDockItem(item);
        }
    }

    public void SaveDockItems()
    {
        var items = new List<DockItem>();
        foreach (Button button in dockPanel.Children)
        {
            if (button.Tag is string filePath && button.Content is StackPanel stackPanel)
            {
                var textBlock = stackPanel.Children[1] as TextBlock; // Der zweite Eintrag sollte das TextBlock sein
                if (textBlock != null)
                {
                    items.Add(new DockItem
                    {
                        FilePath = filePath,
                        DisplayName = textBlock.Text ?? string.Empty,
                        // Kategorie später hinzufügen
                    });
                }
            }
        }
        SettingsManager.SaveSettings(items);
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
            dockPanel.Children.Remove(droppedButton);
            // Bestimme die neue Position
            Point dropPosition = e.GetPosition(dockPanel);
            double dropCenterX = dropPosition.X;
            int newIndex = 0;
            bool inserted = false;
            for (int i = 0; i < dockPanel.Children.Count; i++)
            {
                if (dockPanel.Children[i] is Button button)
                {
                    Point elementPosition = button.TranslatePoint(new Point(0, 0), dockPanel);
                    double elementCenterX = elementPosition.X + (button.ActualWidth / 2);
                    Console.WriteLine($"Element Center: {elementCenterX}, Drop Center: {dropCenterX}"); // Debugging
                    if (dropCenterX < elementCenterX)
                    {
                        dockPanel.Children.Insert(i, droppedButton);
                        inserted = true;
                        Console.WriteLine("Insert at index: " + i); // Debugging
                        break;
                    }
                }
                newIndex++;
            }
            // Wenn das Element noch nicht eingefügt wurde, füge es am Ende hinzu
            if (!inserted)
            {
                dockPanel.Children.Add(droppedButton);
                Console.WriteLine("Added at the end"); // Debugging
            }
            SaveDockItems();
            Console.WriteLine("Drop an Position: " + newIndex); // Debugging
        }
        else
        {
            Console.WriteLine("Kein gültiger Button gedroppt"); // Debugging
        }
    }
    else if (e.Data.GetDataPresent(DataFormats.FileDrop))
    {
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        foreach (var file in files)
        {
            var dockItem = new DockItem
            {
                FilePath = file ?? string.Empty,
                DisplayName = System.IO.Path.GetFileNameWithoutExtension(file) ?? string.Empty,
                // Kategorie später hinzufügen
            };
            AddDockItem(dockItem);
        }
        Console.WriteLine("Externe Dateien abgelegt"); // Debugging
    }
    else
    {
        Console.WriteLine("Kein gültiges Drop-Format erkannt"); // Debugging
    }
}



    public void RemoveDockItem(Button button)
    {
        if (dockPanel.Children.Contains(button))
        {
            dockPanel.Children.Remove(button);
            SaveDockItems();
            Console.WriteLine("Element entfernt und Einstellungen gespeichert."); // Debug-Ausgabe
        }
    }



    private void AddDockItem(DockItem item)
    {
        var icon = IconHelper.GetIcon(item.FilePath);
        var image = new Image
        {
            Source = icon,
            Width = 32,
            Height = 32,
            Margin = new Thickness(5)
        };
        var textBlock = new TextBlock
        {
            Text = item.DisplayName,
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
        var button = new Button
        {
            Content = stackPanel,
            Tag = item.FilePath,
            Margin = new Thickness(5),
            Width = 70
        };

        button.PreviewMouseLeftButtonDown += (s, e) =>
        {
            Console.WriteLine("Button Mouse Down Event ausgelöst"); // Debugging
            dragStartPoint = e.GetPosition(dockPanel);
            draggedButton = button;
            if (draggedButton != null)
            {
                Console.WriteLine("Drag Start: " + draggedButton.Tag); // Debugging
            }
        };

        button.MouseEnter += (s, e) => Console.WriteLine("Maus betritt Element: " + button.Tag);
        button.MouseLeave += (s, e) => Console.WriteLine("Maus verlässt Element: " + button.Tag);

        button.Click += (s, args) =>
        {
            Console.WriteLine("Element geklickt: " + item.FilePath); // Debugging
            mainWindow.OpenFile(item.FilePath);
        };

        // Kontextmenü für die Schaltfläche
        button.MouseRightButtonDown += (s, e) =>
        {
            Console.WriteLine("Rechtsklick auf Element: " + item.DisplayName); // Debugging
            e.Handled = true; // Ereignis als verarbeitet markieren
            mainWindow.OpenMenuItem.Visibility = Visibility.Visible;
            mainWindow.DeleteMenuItem.Visibility = Visibility.Visible;
            mainWindow.EditMenuItem.Visibility = Visibility.Visible;
            mainWindow.DockContextMenu.PlacementTarget = button;
            mainWindow.DockContextMenu.IsOpen = true;
        };

        dockPanel.Children.Add(button);
    }

















}
