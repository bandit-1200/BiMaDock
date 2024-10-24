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
    private bool isDropInProgress = false;

    public DockManager(StackPanel panel, MainWindow window)
    {
        dockPanel = panel;
        mainWindow = window;
        dockPanel.Drop += DockPanel_Drop;
        dockPanel.MouseMove += DockPanel_MouseMove;  // Event-Handler für MouseMove hinzufügen
        dockPanel.MouseEnter += DockPanel_MouseEnter;  // Event-Handler für MouseEnter hinzufügen
        dockPanel.MouseLeave += DockPanel_MouseLeave;  // Event-Handler für MouseLeave hinzufügen
    }

    private void DockPanel_MouseEnter(object sender, MouseEventArgs e)
    {
        mainWindow.ShowDock();
    }

    private void DockPanel_MouseLeave(object sender, MouseEventArgs e)
    {
        if (!mainWindow.IsDragging)
        {
            mainWindow.HideDock();
        }
    }

private void DockPanel_MouseMove(object sender, MouseEventArgs e)
{
    if (dragStartPoint.HasValue && draggedButton != null)
    {
        Point position = e.GetPosition(dockPanel);
        Vector diff = dragStartPoint.Value - position;

        if (e.LeftButton == MouseButtonState.Pressed &&
            (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
        {
            DragDrop.DoDragDrop(draggedButton, new DataObject(DataFormats.Serializable, draggedButton), DragDropEffects.Move);
            dragStartPoint = null;
            draggedButton = null;
        }
    }
    else
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




public void DockPanel_Drop(object sender, DragEventArgs e)
{
    if (isDropInProgress) return; // Doppelte Drop-Verhinderung
    isDropInProgress = true;

    try
    {
        if (e.Data.GetDataPresent(DataFormats.Serializable))
        {
            Button? droppedButton = e.Data.GetData(DataFormats.Serializable) as Button;
            if (droppedButton != null)
            {
                Console.WriteLine("Drop: " + droppedButton.Tag); // Debugging
                dockPanel.Children.Remove(droppedButton);
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
                };
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
                        if (dropCenterX < elementCenterX)
                        {
                            AddDockItemAt(dockItem, i);
                            inserted = true;
                            break;
                        }
                    }
                    newIndex++;
                }
                if (!inserted)
                {
                    AddDockItemAt(dockItem, newIndex);
                }
                Console.WriteLine("Externe Dateien abgelegt"); // Debugging
            }
        }
        else
        {
            Console.WriteLine("Kein gültiges Drop-Format erkannt"); // Debugging
        }
    }
    finally
    {
        isDropInProgress = false; // Flag zurücksetzen
    }

    mainWindow.SetDragging(false); // Dragging-Flag zurücksetzen
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
private void AddDockItemAt(DockItem item, int index)
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

    // Kontextmenü-Event-Handler hinzufügen
    button.MouseRightButtonDown += (s, e) =>
    {
        Console.WriteLine("Rechtsklick auf Element: " + item.DisplayName); // Debugging
        e.Handled = true; // Ereignis als verarbeitet markieren
        mainWindow.OpenMenuItem.Visibility = Visibility.Visible;
        mainWindow.DeleteMenuItem.Visibility = Visibility.Visible;
        mainWindow.EditMenuItem.Visibility = Visibility.Visible;
        mainWindow.DockContextMenu.PlacementTarget = button;
        mainWindow.DockContextMenu.IsOpen = true;
           if (!mainWindow.DockContextMenu.IsOpen)
    {
        mainWindow.ShowDock(); // Dock sichtbar halten
    }
    };

    // Event-Handler für Drag-and-Drop
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

    // Click-Event-Handler hinzufügen
button.Click += (s, e) =>
{
    string? filePath = button.Tag as string;
    if (filePath != null)
    {
        Console.WriteLine("Button Click: " + filePath); // Debugging
        mainWindow.OpenFile(filePath); // Aufruf von OpenFile im MainWindow
    }
    else
    {
        Console.WriteLine("filePath ist null"); // Debugging
    }
};


    dockPanel.Children.Insert(index, button);
    Console.WriteLine($"Element eingefügt an Position: {index}"); // Debugging
    SaveDockItems();
}



    public void AddDockItem(DockItem item)
    {
        AddDockItemAt(item, dockPanel.Children.Count);
    }


}