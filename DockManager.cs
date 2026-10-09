using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using BiMaDock;
using System.Windows.Input;
using System.Windows.Media; // Für SolidColorBrush und Colors
using System.Windows.Media.Animation;
using System.Windows.Controls.Primitives;



public class DockManager
{
    public double mousePositionSave = 0;
    public double mousePositionSaveleft = 0;

    private StackPanel dockPanel;
    private StackPanel? categoryDockContainer; // Referenz zu CategoryDockContainer
    private MainWindow mainWindow;
    private bool isDropInProgress = false;
    private List<string> categories; // Liste zur Verwaltung der Kategorien
    private List<DockItem> dockItems = new List<DockItem>();
    private StackPanel? CategoryDockContainer;
    private Dictionary<Button, bool> animationPlayed = new Dictionary<Button, bool>();



    public DockManager(StackPanel panel, StackPanel categoryPanel, MainWindow window)
    {
        dockPanel = panel;
        categoryDockContainer = categoryPanel; // Zuweisung des Kategorie-Docks
        mainWindow = window;
        dockPanel.MouseMove += DockPanel_MouseMove;  // Event-Handler für MouseMove hinzufügen
        categories = new List<string>(); // Initialisierung der Kategorienliste
        dockItems = new List<DockItem>(); // Initialisierung der Dock-Items-Liste
                                          // categoryDockContainer.PreviewMouseLeftButtonDown += mainWindow.CategoryDockContainer_PreviewMouseLeftButtonDown;
                                          // categoryDockContainer.MouseMove += mainWindow.CategoryDockContainer_MouseMove;

        // Registrierung der Event-Handler für Kategorie-Dock
        categoryDockContainer.MouseMove += mainWindow.CategoryDockContainer_MouseMove;
        categoryDockContainer.Drop += mainWindow.CategoryDockContainer_Drop;
        categoryDockContainer.DragEnter += mainWindow.CategoryDockContainer_DragEnter;
        categoryDockContainer.DragOver += mainWindow.CategoryDockContainer_DragOver;
        categoryDockContainer.DragLeave += mainWindow.CategoryDockContainer_DragLeave;





    }



    private void DockPanel_MouseEnter(object sender, MouseEventArgs e)
    {
        // vorher: mainWindow.ShowDock();
        mainWindow.StartShowDelay();
    }

    private void DockPanel_MouseLeave(object sender, MouseEventArgs e)
    {
        // Abbrechen des geplanten Einblendens, falls noch nicht ausgeführt
        mainWindow.CancelShowDelay();

        if (!mainWindow.isDragging && mainWindow.dockVisible) // Prüfen, ob das Dock sichtbar ist, bevor es ausgeblendet wird
        {
            mainWindow.HideDock();
        }
    }


    private Button? previousButton = null;

    private void DockPanel_MouseMove(object sender, MouseEventArgs e)
    {
        if (!mainWindow.isDragging)
        {
            LogMousePositionAndElements(e.GetPosition(dockPanel));
        }
    }


    // Vorherige Methodenf
    public void LogMousePositionAndElements(Point mousePosition)
    {
        // Debug.WriteLine($"LogMousePositionAndElements: Aktuelle Mausposition: {mousePosition}");

        foreach (var child in dockPanel.Children)
        {
            if (child is Button button && button.Tag is DockItem dockItem)
            {
                var elementRect = new Rect(button.TranslatePoint(new Point(0, 0), dockPanel), button.RenderSize);
                Point elementPosition = button.TranslatePoint(new Point(0, 0), mainWindow);

                // Debug.WriteLine($"LogMousePositionAndElements: Button: ID = {dockItem.Id}, DisplayName = {dockItem.DisplayName}, Position = {elementRect.Location}, Size = {elementRect.Size}");

                if (elementRect.Contains(mousePosition))

                {
                    double elementCenterX = elementPosition.X + (elementRect.Width / 2);
                    double mainWindowCenterX = mainWindow.ActualWidth / 2;
                    double positionRelativeToCenter = elementCenterX - mainWindowCenterX;

                    if (dockItem.IsCategory)
                    {
                        mousePositionSaveleft = elementRect.X;
                        mousePositionSave = positionRelativeToCenter;

                    }

                    if (dockItem.Id == mainWindow.isCategoryDockOpenID)
                    {
                        // Debug.WriteLine("LogMousePositionAndElements: Maus über offener Kategorie");
                        // Setze Margin basierend auf Position
                        if (mainWindow.CategoryDockBorder != null)
                        {
                            // mainWindow.CategoryDockBorder.Margin = new Thickness(elementRect.Location.X, 0, 0, 0);
                        }
                        else
                        {
                            mainWindow.HideCategoryDockPanel();
                        }

                    }






                    if (!animationPlayed.ContainsKey(button) || !animationPlayed[button])
                    {
                        // Debug.WriteLine("LogMousePositionAndElements: Animation wird gestartet");
                        ButtonAnimations.AnimateButtonByChoice(button);  // Animation aufrufen
                        animationPlayed[button] = true;
                    }
                    else if (button != previousButton)
                    {
                        // Debug.WriteLine("LogMousePositionAndElements: Animation wird erneut gestartet");
                        ButtonAnimations.AnimateButtonByChoice(button);  // Animation aufrufen
                    }
                    previousButton = button;

                    // Überprüfe, ob das DockItem eine Kategorie ist und rufe ShowCategoryDockPanel auf
                    if (dockItem.IsCategory)
                    {
                        // Debug.WriteLine("LogMousePositionAndElements :DockItem ist eine Kategorie, rufe ShowCategoryDockPanel auf");
                    }
                    else
                    {
                        // mainWindow.HideCategoryDockPanel();
                    }
                }
                else
                {
                    if (animationPlayed.ContainsKey(button))
                    {
                        // Debug.WriteLine($"LogMousePositionAndElements: Maus verlässt Button: ID = {dockItem.Id}, DisplayName = {dockItem.DisplayName}");
                        animationPlayed[button] = false;
                    }
                }
            }
            else
            {
                // Debug.WriteLine("Kein DockItem an diesem Button gefunden");
            }
        }
    }

    public void InitializeCategoryDockContainer(StackPanel container)
    {
        CategoryDockContainer = container ?? throw new ArgumentNullException(nameof(container), "CategoryDockContainer ist null.");
    }


    public void LoadDockItems()
    {
        // Debug.WriteLine("Lade Dock-Elemente..."); // Debugging zu Beginn des Aufrufs
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
                AddDockItemAt(item, item.Position, item.Category, saveChanges: false);
            }
        }

        // Debug.WriteLine("Dock-Elemente geladen."); // Debugging am Ende des Aufrufs
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


    private Task HandleFileDrop(string[] files, Point dropPosition)
    {
        var dockItemsToAdd = new List<DockItem>(files.Length);
        foreach (var file in files)
        {
            Debug.WriteLine($"DockPanel_Drop: Datei gefunden: {file}");

            dockItemsToAdd.Add(new DockItem
            {
                FilePath = file,
                DisplayName = System.IO.Path.GetFileNameWithoutExtension(file) ?? string.Empty,
            });
        }

        int insertionIndex = DockDropPosition.FindInsertionIndex(dockPanel, dropPosition.X);
        foreach (var dockItem in dockItemsToAdd)
        {
            AddDockItemAt(dockItem, insertionIndex++, string.Empty, saveChanges: false);
        }

        SaveDockItems(string.Empty);
        return Task.CompletedTask;
    }

    private void HandleTextDrop(string rawData, Point dropPosition)
    {
        string url = rawData.ToString();
        var dockItem = new DockItem
        {
            FilePath = url,
            DisplayName = url,
        };
        InsertDockItem(dockItem, dropPosition);
    }


    private void HandleSerializableDrop(Button droppedButton, Point dropPosition)
    {
        if (droppedButton != null && droppedButton.Tag is DockItem droppedItem)
        {
            Debug.WriteLine("DockPanel_Drop: Button gefunden und als DockItem erkannt");

            droppedItem.Category = "";

            var parent = VisualTreeHelper.GetParent(droppedButton) as Panel;
            parent?.Children.Remove(droppedButton);

            InsertDockButton(droppedButton, dropPosition);
            SaveDockItems(string.Empty);
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
        RemovePlaceholders();

        SolidColorBrush? primaryColor = Application.Current.Resources["PrimaryColor"] as SolidColorBrush;
        if (primaryColor != null)
        {
            dockPanel.Background = primaryColor;
        }

        mainWindow.currentDockStatus &= ~MainWindow.DockStatus.DraggingToDock;
        mainWindow.currentDockStatus |= MainWindow.DockStatus.MainDockHover;
        mainWindow.CheckAllConditions();
        mainWindow.SetDragging(false);

        Debug.WriteLine("DockPanel_Drop: Drop-Vorgang abgeschlossen");
        mainWindow.HideCategoryDockPanel();
    }

    private void RemovePlaceholders()
    {
        for (int i = dockPanel.Children.Count - 1; i >= 0; i--)
        {
            if (dockPanel.Children[i] is Border border && border.Tag as string == "Placeholder")
            {
                dockPanel.Children.RemoveAt(i);
            }
        }
    }


    public async void DockPanel_Drop(object sender, DragEventArgs e)
    {
        Debug.WriteLine("DockPanel_Drop: Drop-Vorgang gestartet");

        if (isDropInProgress)
        {
            Debug.WriteLine("DockPanel_Drop: Drop bereits in Bearbeitung, Vorgang abgebrochen");
            return; // Doppelte Drop-Verhinderung
        }

        isDropInProgress = true;
        try
        {
            Point dropPosition = e.GetPosition(dockPanel); // Berechne die Drop-Position einmal und übergebe sie

            RemovePlaceholders();
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
                Debug.WriteLine("DockPanel_Drop: Dateipfade gefunden");
                await HandleFileDrop(files, dropPosition);
            }
            else if (e.Data.GetDataPresent(DataFormats.UnicodeText) || e.Data.GetDataPresent(DataFormats.Text))
            {
                string format = e.Data.GetDataPresent(DataFormats.UnicodeText)
                    ? DataFormats.UnicodeText
                    : DataFormats.Text;
                if (e.Data.GetData(format) is string rawData)
                {
                    HandleTextDrop(rawData, dropPosition);
                }
            }

            e.Handled = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DockPanel_Drop: Fehler aufgetreten - {ex}");
            MessageBox.Show(mainWindow, "Das Element konnte nicht zum Dock hinzugefügt werden.",
                "BiMaDock", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            isDropInProgress = false;
            CleanupAfterDrop();
        }
    }


    private void ListAllDockPanelElements()
    {
        Debug.WriteLine("ListAllDockPanelElements: Aufgerufen"); // Debug-Ausgabe
        for (int i = 0; i < dockPanel.Children.Count; i++)
        {
            UIElement element = dockPanel.Children[i];
            Debug.WriteLine($"ListAllDockPanelElements: Element {i}: Typ = {element.GetType().Name}");

            // Zusätzliche Informationen anzeigen, falls das Element ein Button ist
            if (element is Button button && button.Tag is DockItem dockItem)
            {
                Debug.WriteLine($"ListAllDockPanelElements: Element {i} - DisplayName = {dockItem.DisplayName}, ID = {dockItem.Id}, Kategorie = {dockItem.Category}, IsCategory = {dockItem.IsCategory}");
            }
            // Zusätzliche Informationen anzeigen, falls das Element ein Border ist
            else if (element is Border border)
            {
                Debug.WriteLine($"ListAllDockPanelElements: Element border {i} - Border, Tag = {border.Tag}");

                // Überprüfen, ob der Border als Platzhalter markiert ist
                if (border.Tag as string == "Placeholder")
                {
                    Debug.WriteLine($"ListAllDockPanelElements: Lösche Platzhalter Border bei Index {i}");
                    dockPanel.Children.Remove(border);
                    i--; // Index anpassen, da ein Element entfernt wurde
                }
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

        // Debug.WriteLine($"Alle Kinder der Kategorie '{categoryName}' wurden entfernt und gespeichert."); // Debug-Ausgabe
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
                    mainWindow.SetDragging(true);
                    try
                    {
                        var dragData = new DataObject(DockDropPosition.DockItemButtonFormat, button);
                        DragDrop.DoDragDrop(button, dragData, DragDropEffects.Move);
                    }
                    finally
                    {
                        mainWindow.SetDragging(false);
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
        // Debug.WriteLine("Open_Click aufgerufen"); // Debug-Ausgabe

        if (mainWindow.DockContextMenu.PlacementTarget is Button button)
        {
            // Debug.WriteLine("Button erkannt"); // Debug-Ausgabe

            if (button.Tag is DockItem dockItem)
            {
                // Debug.WriteLine($"DockItem erkannt: {dockItem.DisplayName}"); // Debug-Ausgabe

                if (!string.IsNullOrEmpty(dockItem.FilePath))
                {
                    Debug.WriteLine($"Open_Click aufgerufen, filePath: {dockItem.FilePath}"); // Debug-Ausgabe
                    mainWindow.OpenFile(dockItem.FilePath);
                }
                else
                {
                    Debug.WriteLine("Open_Click aufgerufen, Kategorie"); // Debug-Ausgabe
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
