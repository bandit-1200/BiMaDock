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
    private StackPanel? categoryDockContainer; // Referenz zu CategoryDockContainer
    private MainWindow mainWindow;
    private Point? dragStartPoint = null;  // Definition hinzugefügt
    private Button? draggedButton = null;  // Definition hinzugefügt
    // private bool isDragging = false;       // Definition hinzugefügt
    private bool isDropInProgress = false;
    private List<string> categories; // Liste zur Verwaltung der Kategorien
    private List<DockItem> dockItems = new List<DockItem>();
    private StackPanel? CategoryDockContainer;




    public DockManager(StackPanel panel, StackPanel categoryPanel, MainWindow window)
    {
        dockPanel = panel;
        categoryDockContainer = categoryPanel; // Zuweisung des Kategorie-Docks
        mainWindow = window;
        dockPanel.MouseMove += DockPanel_MouseMove;  // Event-Handler für MouseMove hinzufügen
        dockPanel.MouseEnter += DockPanel_MouseEnter;  // Event-Handler für MouseEnter hinzufügen
        categories = new List<string>(); // Initialisierung der Kategorienliste
        dockItems = new List<DockItem>(); // Initialisierung der Dock-Items-Liste
                                          // categoryDockContainer.PreviewMouseLeftButtonDown += mainWindow.CategoryDockContainer_PreviewMouseLeftButtonDown;
                                          // categoryDockContainer.MouseMove += mainWindow.CategoryDockContainer_MouseMove;

        // Registrierung der Event-Handler für Kategorie-Dock
        categoryDockContainer.PreviewMouseLeftButtonDown += mainWindow.CategoryDockContainer_PreviewMouseLeftButtonDown;
        categoryDockContainer.MouseMove += mainWindow.CategoryDockContainer_MouseMove;
        categoryDockContainer.Drop += mainWindow.CategoryDockContainer_Drop;
        categoryDockContainer.DragEnter += mainWindow.CategoryDockContainer_DragEnter;
        categoryDockContainer.DragLeave += mainWindow.CategoryDockContainer_DragLeave;





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
                        // Console.WriteLine($"Maus über Element: {button.Tag}");
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
                    // Console.WriteLine($"Maus zwischen Elementen: {prevButton.Tag} und {nextButton.Tag}");
                }
                else if (nextElement is Button onlyNextButton)
                {
                    // Console.WriteLine($"Maus vor dem ersten Element: {onlyNextButton.Tag}");
                }
                else if (previousElement is Button onlyPrevButton)
                {
                    // Console.WriteLine($"Maus nach dem letzten Element: {onlyPrevButton.Tag}");
                }
                else
                {
                    // Console.WriteLine("Maus über Dock ohne Element");
                }
            }
        }
    }

    public void InitializeCategoryDockContainer(StackPanel container)
    {
        CategoryDockContainer = container ?? throw new ArgumentNullException(nameof(container), "CategoryDockContainer ist null.");
    }





public void LoadDockItems()
{
    Console.WriteLine("Lade Dock-Elemente..."); // Debugging zu Beginn des Aufrufs
    var items = SettingsManager.LoadSettings();

    // Zuerst alle vorhandenen Items aus den Panels entfernen
    dockPanel.Children.Clear();
    if (categoryDockContainer != null)
    {
        categoryDockContainer.Children.Clear();
    }

    if (items == null || items.Count == 0)
    {
        var explorerItem = new DockItem
        {
            FilePath = @"C:\Windows\explorer.exe",
            DisplayName = "File Explorer",
            Category = "",
            IsCategory = false,
            Position = 0
        };
        AddDockItemAt(explorerItem, 0, explorerItem.Category); // currentCategory übergeben

        var cmdItem = new DockItem
        {
            FilePath = @"C:\Windows\System32\cmd.exe",
            DisplayName = "Command Prompt",
            Category = "",
            IsCategory = false,
            Position = 1
        };
        AddDockItemAt(cmdItem, 1, cmdItem.Category); // currentCategory übergeben
    }
    else
    {
        foreach (var item in items)
        {
            AddDockItemAt(item, item.Position, item.Category); // currentCategory übergeben

            // Sicherstellen, dass Event-Handler gesetzt sind
            if (!string.IsNullOrEmpty(item.Category) && categoryDockContainer != null)
            {
                // Überprüfen, ob das Element im Kategorie-Dock eingefügt wurde
                foreach (Button button in categoryDockContainer.Children)
                {
                    if (button.Tag == item)
                    {
                        button.PreviewMouseLeftButtonDown += mainWindow.CategoryDockContainer_PreviewMouseLeftButtonDown;
                        button.MouseMove += mainWindow.CategoryDockContainer_MouseMove;
                        button.Drop += mainWindow.CategoryDockContainer_Drop;
                        button.DragEnter += mainWindow.CategoryDockContainer_DragEnter;
                        button.DragLeave += mainWindow.CategoryDockContainer_DragLeave;
                    }
                }
            }
        }
    }

    Console.WriteLine("Dock-Elemente geladen."); // Debugging am Ende des Aufrufs
}


public void SaveDockItems(string currentCategory)
{
    if (CategoryDockContainer == null)
    {
        return;
    }

    var items = new List<DockItem>();
    var categoryItems = new List<DockItem>();
    int mainDockIndex = 0;

    foreach (UIElement element in dockPanel.Children)
    {
        if (element is Button button && button.Tag is DockItem dockItem)
        {
            dockItem.Position = mainDockIndex++;
            if (dockItem.IsCategory)
            {
                dockItem.Category = "";
            }
            items.Add(dockItem);
        }
    }

    foreach (UIElement element in CategoryDockContainer.Children)
    {
        if (element is Button button && button.Tag is DockItem dockItem)
        {
            if (!dockItem.IsCategory && dockItem.Category == currentCategory)
            {
                dockItem.Position = categoryItems.Count;
                categoryItems.Add(dockItem);
            }
            else if (dockItem.IsCategory)
            {
                categoryItems.Add(dockItem);
            }
        }
    }

    items.AddRange(categoryItems);
    var existingItems = SettingsManager.LoadSettings();

    // Entfernen aller vorhandenen Elemente der aktuellen Kategorie
    existingItems.RemoveAll(item => item.Category == currentCategory || item.DisplayName == currentCategory);

    foreach (var item in existingItems)
    {
        if (item.Category != currentCategory)
        {
            items.Add(item);
        }
    }

    var uniqueItems = new HashSet<string>();
    items.RemoveAll(item => !uniqueItems.Add(item.Id)); // ID zur Überprüfung von Duplikaten verwenden

    SettingsManager.SaveSettings(items);
}




public void AddCategoryItem(string categoryName)
{
    // Aktuellen Stand der Dock-Settings einlesen
    var existingItems = SettingsManager.LoadSettings();

    // Überprüfen, ob die Kategorie bereits existiert
    var existingCategoryItem = existingItems.FirstOrDefault(item => item.DisplayName == categoryName && item.IsCategory);

    if (existingCategoryItem != null)
    {
        MessageBox.Show($"Kategorie '{categoryName}' existiert bereits.", "Kategorie existiert", MessageBoxButton.OK, MessageBoxImage.Warning);
        return; // Kategorie nicht erneut erstellen
    }

    // Neue Kategorie erstellen
    var categoryItem = new DockItem
    {
        Id = Guid.NewGuid().ToString(),
        FilePath = "",
        DisplayName = categoryName,
        Category = "",
        IsCategory = true,
        IconSource = "" // IconSource bleibt leer beim ersten Anlegen
    };
    AddDockItemAt(categoryItem, dockPanel.Children.Count, categoryItem.DisplayName);

    // Erstelle und füge ein neues "cmd"-Element zur neuen Kategorie hinzu
    var cmdItem = new DockItem
    {
        Id = Guid.NewGuid().ToString(), // Neue eindeutige ID für das "Command Prompt"-Element
        FilePath = @"C:\Windows\System32\cmd.exe",
        DisplayName = "Command Prompt",
        Category = categoryName, // Setze die neue Kategorie
        IsCategory = false,
        IconSource = "" // cmdItem hat keine IconSource
    };
    AddDockItemAt(cmdItem, 0, categoryName); // Füge neues cmdItem hinzu

    // Speichern der aktualisierten Settings mit den neuen Elementen
    existingItems.Add(categoryItem);
    existingItems.Add(cmdItem);
    SettingsManager.SaveSettings(existingItems);
}



    public void DockPanel_Drop(object sender, DragEventArgs e)
    {
        if (isDropInProgress) return; // Doppelte Drop-Verhinderung
                                      // isDropInProgress = true;
        try
        {
            if (e.Data.GetDataPresent(DataFormats.Serializable))
            {
                Button? droppedButton = e.Data.GetData(DataFormats.Serializable) as Button;
                if (droppedButton != null && droppedButton.Tag is DockItem droppedItem)
                {
                    // Überprüfung auf Kategorie
                    if (!string.IsNullOrEmpty(droppedItem.Category))
                    {
                        // Setze die Kategorie auf leer, um das Element aus der Kategorie zu entfernen
                        droppedItem.Category = "";
                    }
                    // Element sicher vom Elternteil trennen
                    var parent = VisualTreeHelper.GetParent(droppedButton) as Panel;
                    if (parent != null)
                    {
                        parent.Children.Remove(droppedButton);
                    }

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
                                break;
                            }
                        }
                        newIndex++;
                    }
                    if (!inserted)
                    {
                        dockPanel.Children.Add(droppedButton);
                    }
                    // Aktualisiere und speichere die Dock-Items nach dem Verschieben
                    SaveDockItems(droppedItem.Category);
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
                                AddDockItemAt(dockItem, i, dockItem.Category);
                                inserted = true;
                                break;
                            }
                        }
                        newIndex++;
                    }
                    if (!inserted)
                    {
                        AddDockItemAt(dockItem, newIndex, dockItem.Category);
                    }
                }
            }
        }
        finally
        {
            isDropInProgress = false;
            SolidColorBrush? primaryColor = (SolidColorBrush?)new BrushConverter().ConvertFromString("#1E1E1E"); // Die ursprüngliche PrimaryColor direkt setzen

            if (primaryColor != null)
            {
                dockPanel.Background = primaryColor;
            }

            mainWindow.currentDockStatus &= ~MainWindow.DockStatus.DraggingToDock;
            mainWindow.currentDockStatus |= MainWindow.DockStatus.MainDockHover;
            mainWindow.CheckAllConditions();
        }


        mainWindow.SetDragging(false);
    }




    public void UpdateDockItemLocation(Button button, string currentCategory)
    {
        var dockItem = button.Tag as DockItem;
        if (dockItem != null)
        {
            // Aktualisiere die Position des Dock-Items
            var dockItems = SettingsManager.LoadSettings();
            var itemToUpdate = dockItems.FirstOrDefault(di => di.Id == dockItem.Id);
            if (itemToUpdate != null)
            {
                // Update the position or any other property if needed
                itemToUpdate.Position = dockItem.Position;
                SettingsManager.SaveSettings(dockItems); // Save the updated dockItems
            }
        }
    }



    public void RemoveDockItem(Button button, string currentCategory)
    {
        if (button.Tag is DockItem dockItem)
        {
            if (string.IsNullOrEmpty(currentCategory))
            {
                // Element aus Hauptdock entfernen
                dockPanel.Children.Remove(button);

                // Auch alle Kindelemente entfernen, die zu dieser Kategorie gehören
                RemoveCategoryChildren(dockItem.DisplayName);
            }
            else
            {

                // Element aus Kategorie-Dock entfernen
                if (categoryDockContainer != null)
                {
                    categoryDockContainer.Children.Remove(button);
                }
            }

            // Aktualisiere und speichere die Dock-Items nach dem Löschen
            SaveDockItems(currentCategory);
        }
        SaveDockItems(currentCategory);
    }




    private void RemoveCategoryChildren(string categoryName)
    {
        // Laden der aktuellen Dock-Items
        var items = SettingsManager.LoadSettings();

        // Sammeln der zu entfernenden Elemente
        var itemsToRemove = new List<DockItem>();

        foreach (var item in items)
        {
            if (item.Category == categoryName)
            {
                itemsToRemove.Add(item);
            }
        }

        // Entfernen der gesammelten Elemente aus der Liste
        foreach (var item in itemsToRemove)
        {
            items.Remove(item);
        }

        // Speichern der aktualisierten Dock-Items
        SettingsManager.SaveSettings(items);

        Console.WriteLine($"Alle Kinder der Kategorie '{categoryName}' wurden entfernt und gespeichert."); // Debug-Ausgabe
    }






public void AddDockItemAt(DockItem item, int index, string currentCategory)
{
    // Check if the item is a category and has an IconSource
    var iconSource = item.IsCategory && !string.IsNullOrEmpty(item.IconSource) ? item.IconSource : item.FilePath;
    var icon = IconHelper.GetIcon(iconSource);

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
        TextWrapping = TextWrapping.NoWrap,
        TextTrimming = TextTrimming.CharacterEllipsis,
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
        Tag = item,
        Margin = new Thickness(5),
        Width = 70
    };
    button.MouseRightButtonDown += (s, e) =>
    {
        e.Handled = true;
        mainWindow.OpenMenuItem.Visibility = Visibility.Visible;
        mainWindow.DeleteMenuItem.Visibility = Visibility.Visible;
        mainWindow.EditMenuItem.Visibility = Visibility.Visible;
        mainWindow.DockContextMenu.PlacementTarget = button;
        mainWindow.DockContextMenu.IsOpen = true;
        mainWindow.currentDockStatus |= MainWindow.DockStatus.ContextMenuOpen;
        mainWindow.CheckAllConditions();
    };
    button.PreviewMouseLeftButtonDown += (s, e) =>
    {
        dragStartPoint = e.GetPosition(button);
        draggedButton = button;
    };
    button.PreviewMouseMove += (s, e) =>
    {
        if (dragStartPoint.HasValue && draggedButton == button)
        {
            Point position = e.GetPosition(button);
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
    };
    button.Click += (s, e) =>
    {
        var dockItem = button.Tag as DockItem;
        if (dockItem != null)
        {
            mainWindow.OpenDockItem(dockItem);
        }
    };
    if (!string.IsNullOrEmpty(item.Category))
    {
        if (categoryDockContainer != null)
        {
            int adjustedIndex = Math.Clamp(index, 0, categoryDockContainer.Children.Count);
            categoryDockContainer.Children.Insert(adjustedIndex, button);
        }
    }
    else
    {
        int adjustedIndex = Math.Clamp(index, 0, dockPanel.Children.Count);
        dockPanel.Children.Insert(adjustedIndex, button);
    }
    SaveDockItems(currentCategory);
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

    // public void AddDockItem(DockItem item)
    // {
    //     AddDockItemAt(item, dockPanel.Children.Count);
    // }

    // public void InitializeCategoryDock(string categoryName)
    // {
    //     Console.WriteLine("InitializeCategoryDock aufgerufen"); // Debug-Ausgabe

    //     // Erstelle ein Standard-Dock-Item und weise es der Kategorie zu
    //     var cmdItem = new DockItem
    //     {
    //         FilePath = @"C:\Windows\System32\cmd.exe",
    //         DisplayName = "Command Prompt",
    //         Category = categoryName, // Zuweisung zur spezifischen Kategorie
    //         IsCategory = false,
    //         Position = 0
    //     };

    //     // Füge das Standard-Element zum Kategoriedock hinzu
    //     AddDockItemAt(cmdItem, 0, categoryName);
    //     Console.WriteLine($"Standard-Element 'Command Prompt' zur Kategorie {categoryName} hinzugefügt"); // Debug-Ausgabe
    // }





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