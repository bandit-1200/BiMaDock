using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using BiMaDock;
using System.Windows.Input;
using System.Windows.Media; // Für SolidColorBrush und Colors



public class DockManager
{

    private StackPanel dockPanel;
    private readonly StackPanel categoryDockContainer; // Referenz zu CategoryDockContainer
    private MainWindow mainWindow;



    public DockManager(StackPanel panel, StackPanel categoryPanel, MainWindow window)
    {
        dockPanel = panel;
        categoryDockContainer = categoryPanel; // Zuweisung des Kategorie-Docks
        mainWindow = window;
        dockPanel.MouseMove += DockPanel_MouseMove;  // Event-Handler für MouseMove hinzufügen

        // Registrierung der Event-Handler für Kategorie-Dock
        categoryDockContainer.Drop += mainWindow.CategoryDockContainer_Drop;
        categoryDockContainer.DragEnter += mainWindow.CategoryDockContainer_DragEnter;
        categoryDockContainer.DragOver += mainWindow.CategoryDockContainer_DragOver;
        categoryDockContainer.DragLeave += mainWindow.CategoryDockContainer_DragLeave;





    }



    // Button, über dem sich die Maus aktuell befindet (Animation wurde für diesen Hover bereits abgespielt)
    private Button? hoveredButton = null;

    private void DockPanel_MouseMove(object sender, MouseEventArgs e)
    {
        if (!mainWindow.isDragging)
        {
            LogMousePositionAndElements(e.GetPosition(dockPanel));
        }
    }


    public void LogMousePositionAndElements(Point mousePosition)
    {

        Button? matchedButton = null;

        foreach (var child in dockPanel.Children)
        {
            if (child is Button button && button.Tag is DockItem dockItem)
            {
                var elementRect = new Rect(button.TranslatePoint(new Point(0, 0), dockPanel), button.RenderSize);

                if (elementRect.Contains(mousePosition))
                {
                    matchedButton = button;

                    if (dockItem.Id == mainWindow.isCategoryDockOpenID)
                    {
                        if (mainWindow.CategoryDockBorder == null)
                        {
                            mainWindow.HideCategoryDockPanel();
                        }
                    }






                    // Animation nur einmal pro Hover (beim Betreten des Buttons) abspielen
                    if (!ReferenceEquals(hoveredButton, button))
                    {
                        ButtonAnimations.AnimateButtonByChoice(button);  // Animation aufrufen
                    }
                    break; // Buttons überlappen nicht – weitere Prüfungen sind unnötig
                }
            }
        }

        // Kein Button unter der Maus: Hover-Zustand zurücksetzen
        hoveredButton = matchedButton;
    }


    public void LoadDockItems()
    {
        var items = SettingsManager.LoadSettings();

        // Referenzen auf alte Buttons freigeben, damit sie nicht im Speicher gehalten werden
        hoveredButton = null;

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
                Position = 0,
                IconSource = ""
            };
            AddDockItemAt(explorerItem, 0, explorerItem.Category, saveChanges: false);

            var cmdItem = new DockItem
            {
                FilePath = @"C:\Windows\System32\cmd.exe",
                DisplayName = "Command Prompt",
                Category = "",
                IsCategory = false,
                Position = 1,
                IconSource = ""
            };
            AddDockItemAt(cmdItem, 1, cmdItem.Category, saveChanges: false);
            SaveDockItems(string.Empty);
        }
        else
        {
            foreach (var item in items)
            {
                try
                {
                    AddDockItemAt(item, item.Position, item.Category, saveChanges: false);
                }
                catch (Exception ex)
                {
                    // Ein fehlerhaftes Element soll das Laden der übrigen nicht abbrechen
                    Debug.WriteLine($"Fehler beim Laden des Dock-Elements '{item.DisplayName}': {ex.Message}");
                }
            }
        }

    }

    public void SaveDockItems(string currentCategory)
    {
        var items = new List<DockItem>();
        var categoryItems = new List<DockItem>();
        int mainDockIndex = 0;

        // Hauptdock-Elemente speichern
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

        // Kategorie-Dock-Elemente speichern
        foreach (UIElement element in categoryDockContainer.Children)
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

        // Vermeiden von doppelten Einträgen und sicherstellen, dass bestehende Elemente korrekt gespeichert werden
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
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            throw new ArgumentException("Der Kategoriename darf nicht leer sein.", nameof(categoryName));
        }

        // Aktuellen Stand der Dock-Settings einlesen
        var existingItems = SettingsManager.LoadSettings();

        // Neue Kategorie erstellen
        var categoryItem = new DockItem
        {
            Id = Guid.NewGuid().ToString(),
            FilePath = "",
            DisplayName = categoryName.Trim(),
            Category = "",
            IsCategory = true,
            IconSource = "", // IconSource bleibt leer beim ersten Anlegen
        };
        // Erstelle das Start-Element der neuen Kategorie.
        var cmdItem = new DockItem
        {
            Id = Guid.NewGuid().ToString(), // Neue eindeutige ID für das "Command Prompt"-Element
            FilePath = @"C:\Windows\System32\cmd.exe",
            DisplayName = "Command Prompt",
            Category = categoryItem.Id, // Setze die neue Kategorie
            IsCategory = false,
            IconSource = "" // cmdItem hat keine IconSource
        };
        existingItems.Add(categoryItem);
        existingItems.Add(cmdItem);
        SettingsManager.SaveSettings(existingItems);
        AddDockItemAt(categoryItem, dockPanel.Children.Count, string.Empty, saveChanges: false);
    }


    private void HandleFileDrop(string[] files, Point dropPosition)
    {
        // Bereits im Hauptdock vorhandene Pfade überspringen (Groß-/Kleinschreibung egal)
        var existingPaths = new HashSet<string>(
            dockPanel.Children.OfType<Button>()
                .Select(button => button.Tag)
                .OfType<DockItem>()
                .Where(item => string.IsNullOrEmpty(item.Category) && !string.IsNullOrEmpty(item.FilePath))
                .Select(item => item.FilePath),
            StringComparer.OrdinalIgnoreCase);

        var dockItemsToAdd = new List<DockItem>(files.Length);
        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file) || !existingPaths.Add(file))
            {
                continue;
            }

            // .lnk-Dateien bleiben als Pfad erhalten (Start funktioniert), der Name kommt ohne Endung
            dockItemsToAdd.Add(new DockItem
            {
                FilePath = file,
                DisplayName = DockDropPosition.GetDisplayName(file),
            });
        }

        if (dockItemsToAdd.Count == 0)
        {
            return;
        }

        int insertionIndex = DockDropPosition.FindInsertionIndex(dockPanel, dropPosition.X);
        foreach (var dockItem in dockItemsToAdd)
        {
            AddDockItemAt(dockItem, insertionIndex++, string.Empty, saveChanges: false);
        }

        SaveDockItems(string.Empty);
    }

    private void HandleLinkDrop(string link, Point dropPosition)
    {
        var dockItem = new DockItem
        {
            FilePath = link,
            DisplayName = DockDropPosition.GetDisplayName(link),
        };
        InsertDockItem(dockItem, dropPosition);
    }


    private void HandleSerializableDrop(Button droppedButton, Point dropPosition)
    {
        if (droppedButton != null && droppedButton.Tag is DockItem droppedItem)
        {
            var parent = VisualTreeHelper.GetParent(droppedButton) as Panel;
            int oldIndex = parent?.Children.IndexOf(droppedButton) ?? -1;
            string oldCategory = droppedItem.Category;

            try
            {
                parent?.Children.Remove(droppedButton);
                InsertDockButton(droppedButton, dropPosition);
                droppedItem.Category = "";
                SaveDockItems(string.Empty);
            }
            catch
            {
                // Rückgängig machen, damit Oberfläche und Daten konsistent bleiben
                droppedItem.Category = oldCategory;
                if (VisualTreeHelper.GetParent(droppedButton) is Panel currentParent)
                {
                    currentParent.Children.Remove(droppedButton);
                }

                if (parent != null && oldIndex >= 0)
                {
                    parent.Children.Insert(Math.Clamp(oldIndex, 0, parent.Children.Count), droppedButton);
                }

                throw;
            }
        }
    }


    private void InsertDockItem(DockItem dockItem, Point dropPosition)
    {
        int insertionIndex = DockDropPosition.FindInsertionIndex(dockPanel, dropPosition.X);
        AddDockItemAt(dockItem, insertionIndex, dockItem.Category);
    }

    private void InsertDockButton(Button droppedButton, Point dropPosition)
    {
        int insertionIndex = DockDropPosition.FindInsertionIndex(dockPanel, dropPosition.X);
        dockPanel.Children.Insert(insertionIndex, droppedButton);
    }

    private void CleanupAfterDrop()
    {
        // Platzhalter, Hintergründe und DraggingToDock-Flag zentral im MainWindow zurücksetzen
        mainWindow.CleanupAfterDrag();

        mainWindow.currentDockStatus |= MainWindow.DockStatus.MainDockHover;
        mainWindow.CheckAllConditions();
        mainWindow.SetDragging(false);

        mainWindow.HideCategoryDockPanel();
    }

    public void DockPanel_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        try
        {
            Point dropPosition = e.GetPosition(dockPanel); // Berechne die Drop-Position einmal und übergebe sie

            // Platzhalter vor der Positionsberechnung entfernen
            mainWindow.CleanupAfterDrag();
            if (e.Data.GetDataPresent(DockDropPosition.DockItemButtonFormat))
            {
                Button? droppedButton = e.Data.GetData(DockDropPosition.DockItemButtonFormat) as Button;
                if (droppedButton != null)
                {
                    HandleSerializableDrop(droppedButton, dropPosition);
                }
            }
            else if (DockDropPosition.GetDroppedFilePaths(e.Data) is string[] files)
            {
                HandleFileDrop(files, dropPosition);
            }
            else if (DockDropPosition.TryGetLinkText(e.Data, out string link))
            {
                HandleLinkDrop(link, dropPosition);
            }
            // Ungültiger Text wird ignoriert
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DockPanel_Drop: Fehler aufgetreten - {ex}");
            DialogMessageBox.Show(mainWindow, "Das Element konnte nicht zum Dock hinzugefügt werden.",
                "BiMaDock", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            CleanupAfterDrop();
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
                RemoveCategoryChildren(dockItem.Id);
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
    }


    private void RemoveCategoryChildren(string categoryId)
    {
        // Laden der aktuellen Dock-Items
        var items = SettingsManager.LoadSettings();

        // Sammeln der zu entfernenden Elemente
        var itemsToRemove = new List<DockItem>();

        foreach (var item in items)
        {
            if (item.Category == categoryId)
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

    }



    public void AddDockItemAt(DockItem item, int index, string currentCategory, bool saveChanges = true)
    {
        var button = DockItemButtonFactory.Create(item);
        button.MouseRightButtonDown += (s, e) =>
        {
            e.Handled = true;
            mainWindow.OpenMenuItem.Visibility = Visibility.Visible;
            mainWindow.DeleteMenuItem.Visibility = Visibility.Visible;
            mainWindow.EditMenuItem.Visibility = Visibility.Visible;
            mainWindow.FindFileItem.Visibility = Visibility.Visible;
            mainWindow.DockContextMenu.PlacementTarget = button;
            mainWindow.DockContextMenu.IsOpen = true;
            mainWindow.currentDockStatus |= MainWindow.DockStatus.ContextMenuOpen;
            mainWindow.CheckAllConditions();
        };
        button.PreviewMouseLeftButtonDown += (s, e) =>
        {
            mainWindow.ActiveDragStartPoint = e.GetPosition(button);
            mainWindow.ActiveDraggedButton = button;
        };
        button.PreviewMouseLeftButtonUp += (s, e) =>
        {
            if (mainWindow.ActiveDraggedButton == button)
            {
                mainWindow.ActiveDragStartPoint = null;
                mainWindow.ActiveDraggedButton = null;
            }
        };
        button.PreviewMouseMove += (s, e) =>
        {
            if (mainWindow.ActiveDragStartPoint.HasValue && mainWindow.ActiveDraggedButton == button)
            {
                Point position = e.GetPosition(button);
                Vector diff = mainWindow.ActiveDragStartPoint.Value - position;
                if (e.LeftButton == MouseButtonState.Pressed &&
                    (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                     Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    mainWindow.ActiveDragStartPoint = null;
                    mainWindow.ActiveDraggedButton = null;
                    // Abbild erzeugen, solange der Button noch sichtbar ist; folgt dem Mauszeiger
                    // während DoDragDrop und wird beim Verlassen des Blocks (auch bei Esc/Ausnahme) geschlossen
                    using var dragGhost = DragGhost.TryCreate(button);
                    mainWindow.SetDragging(true);
                    try
                    {
                        var dragData = new DataObject(DockDropPosition.DockItemButtonFormat, button);
                        DragDrop.DoDragDrop(button, dragData, DragDropEffects.Move);
                    }
                    finally
                    {
                        mainWindow.SetDragging(false);
                        // Platzhalter und Hervorhebungen auch bei Abbruch (Esc) oder Drop außerhalb entfernen
                        mainWindow.CleanupAfterDrag();
                    }
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
        if (saveChanges)
        {
            SaveDockItems(currentCategory);
        }
    }


    public void Open_Click(object sender, RoutedEventArgs e)
    {

        if (mainWindow.DockContextMenu.PlacementTarget is Button button)
        {

            if (button.Tag is DockItem dockItem)
            {

                if (!string.IsNullOrEmpty(dockItem.FilePath))
                {
                    mainWindow.OpenFile(dockItem.FilePath);
                }
                else
                {
                    mainWindow.ShowCategoryDockPanel(new StackPanel { Tag = dockItem.Id });
                }
            }
            else
            {
                Debug.WriteLine("Fehler: button.Tag ist kein DockItem"); // Debug-Ausgabe
            }
        }
        else
        {
            Debug.WriteLine("Fehler: DockContextMenu.PlacementTarget ist kein Button"); // Debug-Ausgabe
        }
    }

}
