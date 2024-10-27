using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using MyDockApp;
using System.Windows.Input;
using System.Windows.Media; // Für SolidColorBrush und Colors


public class DockManager
{


    private StackPanel dockPanel;
    private MainWindow mainWindow;
    private Point? dragStartPoint = null;  // Definition hinzugefügt
    private Button? draggedButton = null;  // Definition hinzugefügt
    private bool isDropInProgress = false;
    private List<string> categories; // Liste zur Verwaltung der Kategorien
    // private List<DockItem> dockItems; // Liste zur Verwaltung der Dock-Items
    private List<DockItem> dockItems = new List<DockItem>();
    public DockManager(StackPanel panel, MainWindow window)



    {
        dockPanel = panel;
        mainWindow = window;
        dockPanel.Drop += DockPanel_Drop;
        dockPanel.MouseMove += DockPanel_MouseMove;  // Event-Handler für MouseMove hinzufügen
        dockPanel.MouseEnter += DockPanel_MouseEnter;  // Event-Handler für MouseEnter hinzufügen
        // dockPanel.MouseLeave += DockPanel_MouseLeave;  // Event-Handler für MouseLeave hinzufügen
        categories = new List<string>(); // Initialisierung der Kategorienliste
        dockItems = new List<DockItem>(); // Initialisierung der Dock-Items-Liste
    }

    private void DockPanel_MouseEnter(object sender, MouseEventArgs e)
    {
        mainWindow.ShowDock();
    }

    private void DockPanel_MouseLeave(object sender, MouseEventArgs e)
    {
        if (!mainWindow.isDragging && mainWindow.dockVisible) // Prüfen, ob das Dock sichtbar ist, bevor es ausgeblendet wird
        {
            Console.WriteLine("DockPanel verlassen, HideDock wird aufgerufen"); // Debugging
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
        Console.WriteLine("Lade Dock-Elemente..."); // Debugging

        var items = SettingsManager.LoadSettings();

        if (items == null || items.Count == 0)
        {
            var explorerItem = new DockItem
            {
                FilePath = @"C:\Windows\explorer.exe",
                DisplayName = "File Explorer",
                Category = ""
            };
            AddDockItemAt(explorerItem, 0);

            var cmdItem = new DockItem
            {
                FilePath = @"C:\Windows\System32\cmd.exe",
                DisplayName = "Command Prompt",
                Category = ""
            };
            AddDockItemAt(cmdItem, 1);
        }
        else
        {
            foreach (var item in items)
            {
                AddDockItemAt(item, dockPanel.Children.Count);
            }
        }
        Console.WriteLine("Dock-Elemente geladen."); // Debugging
    }


    public void SaveDockItems()
    {
        var items = new List<DockItem>();
        foreach (Button button in dockPanel.Children)
        {
            // Hier können wir debuggen, ob jedes Button-Tag korrekt als DockItem erkannt wird
            if (button.Tag is DockItem dockItem)
            {
                Console.WriteLine($"Speichern: {dockItem.DisplayName}, Path: {dockItem.FilePath}, Category: {dockItem.Category}"); // Debugging
                items.Add(dockItem);
            }
            else
            {
                Console.WriteLine("Button-Tag ist kein DockItem"); // Debugging
            }
        }
        SettingsManager.SaveSettings(items);
    }





    public void AddCategoryItem(string categoryName)
    {
        var categoryItem = new DockItem
        {
            FilePath = "",
            DisplayName = categoryName,
            Category = ""
        };

        InitializeCategoryDock(); // Kategorie-Dock initialisieren und anzeigen

        AddDockItemAt(categoryItem, dockPanel.Children.Count);
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
                if (droppedButton != null && droppedButton.Tag is DockItem droppedItem)
                {
                    Point dropPosition = e.GetPosition(dockPanel);
                    bool droppedOnCategory = false;

                    foreach (Button button in dockPanel.Children)
                    {
                        if (button.Tag is DockItem categoryItem && string.IsNullOrEmpty(categoryItem.FilePath) &&
                            categoryItem.DisplayName == droppedItem.Category)
                        {
                            droppedItem.Category = categoryItem.DisplayName;
                            droppedOnCategory = true;
                            break;
                        }
                    }

                    if (droppedOnCategory)
                    {
                        Console.WriteLine("Element zu Kategorie hinzugefügt: " + droppedItem.DisplayName);
                        SaveDockItems();
                        ShowCategoryDock(droppedItem);
                    }
                    else
                    {
                        dockPanel.Children.Remove(droppedButton);
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

    public void UpdateDockItemLocation(Button button)
    {
        var dockItem = button.Tag as DockItem;
        if (dockItem != null)
        {
            // Aktualisiere die Position des Dock-Items
            dockItems.Remove(dockItem);
            dockItems.Add(dockItem);
            SaveDockItems();
            Console.WriteLine($"DockItem {dockItem.DisplayName} aktualisiert"); // Debug-Ausgabe
        }
        else
        {
            Console.WriteLine("DockItem ist null"); // Debug-Ausgabe
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
            Tag = item,  // Das gesamte DockItem als Tag verwenden
            Margin = new Thickness(5),
            Width = 70
        };

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

        button.PreviewMouseLeftButtonDown += (s, e) =>
        {
            Console.WriteLine("Button Mouse Down Event ausgelöst"); // Debugging
            dragStartPoint = e.GetPosition(dockPanel);
            draggedButton = button;
            if (draggedButton != null)
            {
                Console.WriteLine("Drag Start: " + ((DockItem)draggedButton.Tag).FilePath); // Debugging
            }
        };

        // Click-Event-Handler hinzufügen
        button.Click += (s, e) =>
        {
            var dockItem = button.Tag as DockItem;
            if (dockItem != null)
            {
                mainWindow.OpenDockItem(dockItem); // Aufruf von OpenDockItem im MainWindow
            }
            else
            {
                Console.WriteLine("DockItem ist null"); // Debugging
            }
        };

        dockPanel.Children.Insert(index, button);
        Console.WriteLine($"Element eingefügt an Position: {index}"); // Debugging
        SaveDockItems();
    }

    public void Open_Click(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("Open_Click aufgerufen"); // Debug-Ausgabe

        if (mainWindow.DockContextMenu.PlacementTarget is Button button)
        {
            Console.WriteLine("Button erkannt"); // Debug-Ausgabe

            if (button.Tag is DockItem dockItem)
            {
                Console.WriteLine($"DockItem erkannt: {dockItem.DisplayName}"); // Debug-Ausgabe

                if (!string.IsNullOrEmpty(dockItem.FilePath))
                {
                    Console.WriteLine($"Open_Click aufgerufen, filePath: {dockItem.FilePath}"); // Debug-Ausgabe
                    mainWindow.OpenFile(dockItem.FilePath);
                }
                else
                {
                    Console.WriteLine("Open_Click aufgerufen, Kategorie"); // Debug-Ausgabe
                    mainWindow.ShowCategoryDockPanel(new StackPanel
                    {
                        Children = { new Button { Content = $"Kategorie: {dockItem.DisplayName}", Width = 100, Height = 50 } }
                    });
                }
            }
            else
            {
                Console.WriteLine("Fehler: button.Tag ist kein DockItem"); // Debug-Ausgabe
            }
        }
        else
        {
            Console.WriteLine("Fehler: DockContextMenu.PlacementTarget ist kein Button"); // Debug-Ausgabe
        }
    }

    public void AddDockItem(DockItem item)
    {
        AddDockItemAt(item, dockPanel.Children.Count);
    }

    private void InitializeCategoryDock()
    {
        var categoryDock = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Background = new SolidColorBrush(Colors.LightGray),
            Margin = new Thickness(5),
            Visibility = Visibility.Visible // Ständige Sichtbarkeit zum Testen
        };

        // Beispiel-Items zur Demonstration
        var exampleItem = new Button
        {
            Content = "Example Item",
            Width = 150,
            Height = 30,
            Margin = new Thickness(5)
        };

        categoryDock.Children.Add(exampleItem);
        mainWindow.ShowCategoryDockPanel(categoryDock);
    }


    private void ShowCategoryDock(DockItem categoryItem)
    {
        Console.WriteLine($"Kategorie anzeigen geklickt: {categoryItem.DisplayName}"); // Debugging

        var categoryDock = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Background = new SolidColorBrush(Colors.LightGray),
            Margin = new Thickness(5)
        };

        // Beispieldaten für die Elemente der Kategorie
        var dockItems = new List<DockItem>
    {
        new DockItem { DisplayName = "Element in Kategorie", FilePath = "Pfad zu Element", Category = categoryItem.DisplayName }
    };

        foreach (var item in dockItems)
        {
            if (item.Category == categoryItem.DisplayName)
            {
                var button = new Button
                {
                    Content = item.DisplayName,
                    Tag = item,
                    Margin = new Thickness(5)
                };

                button.Click += (s, e) =>
                {
                    string? filePath = item.FilePath;
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        Console.WriteLine("Button Click: " + filePath); // Debugging
                        mainWindow.OpenFile(filePath); // Aufruf von OpenFile im MainWindow
                    }
                };

                categoryDock.Children.Add(button);
            }
        }

        mainWindow.ShowCategoryDockPanel(categoryDock); // Aufruf der Methode im MainWindow
    }




}