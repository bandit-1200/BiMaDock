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
    private Point? dragStartPoint = null;  // Definition hinzugefügt
    private Button? draggedButton = null;  // Definition hinzugefügt
    // private bool isDragging = false;       // Definition hinzugefügt
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
            // Debug.WriteLine("DockPanel verlassen, HideDock wird aufgerufen"); // Debugging
            mainWindow.HideDock();
        }
    }


    private Button? previousButton = null;

    private void DockPanel_MouseMove(object sender, MouseEventArgs e)
    {
        Point mousePosition = e.GetPosition(dockPanel);
        LogMousePositionAndElements(mousePosition);

        // Erhalte die Position der Maus relativ zum MainWindow
        Point windowMousePosition = e.GetPosition(mainWindow);

        // Konvertiere die Position relativ zum MainWindow in Bildschirmkoordinaten
        Point screenPosition = mainWindow.PointToScreen(windowMousePosition);
        // Ausgabe der Bildschirmkoordinaten
        // Debug.WriteLine($"DockPanel_MouseMove: Mausposition auf dem Bildschirm: X = {screenPosition.X}, Y = {screenPosition.Y}");
        // Erhalte die Breite des Hauptbildschirms
        double screenWidth = SystemParameters.PrimaryScreenWidth;

        // Ausgabe der Bildschirmbreite
        // Debug.WriteLine($"DockPanel_MouseMove: Bildschirmbreite: {screenWidth}");
        // Erhalte die Position des Canvas relativ zum MainWindow
        Point canvasPosition = mainWindow.OverlayCanvas.TranslatePoint(new Point(0, 0), mainWindow);

        // Konvertiere die Position relativ zum MainWindow in Bildschirmkoordinaten
        Point canvasScreenPosition = mainWindow.PointToScreen(canvasPosition);
        // Ausgabe der Bildschirmkoordinaten
        // Debug.WriteLine($"DockPanel_MouseMove: Canvasposition auf dem Bildschirm: X = {canvasScreenPosition.X}, Y = {canvasScreenPosition.Y}");



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
                        isOverElement = true;
                        if (!animationPlayed.ContainsKey(button) || !animationPlayed[button])
                        {
                            ButtonAnimations.AnimateButtonByChoice(button); // Methode aus der neuen Klasse aufrufen
                            animationPlayed[button] = true;
                        }
                        else if (button != previousButton)
                        {
                            ButtonAnimations.AnimateButtonByChoice(button); // Methode aus der neuen Klasse aufrufen
                        }
                        previousButton = button;
                        break;
                    }
                    else
                    {
                        previousElement = (i > 0) ? dockPanel.Children[i - 1] : null;
                        nextElement = dockPanel.Children[i];
                    }
                }
            }

            if (!isOverElement)
            {
                previousButton = null;
                if (previousElement is Button prevButton && nextElement is Button nextButton)
                {
                    // Debug.WriteLine($"Maus zwischen Elementen: {prevButton.Tag} und {nextButton.Tag}");
                }
                else if (nextElement is Button onlyNextButton)
                {
                    // Debug.WriteLine($"Maus vor dem ersten Element: {onlyNextButton.Tag}");
                }
                else if (previousElement is Button onlyPrevButton)
                {
                    // Debug.WriteLine($"Maus nach dem letzten Element: {onlyPrevButton.Tag}");
                }
                else
                {
                    // Debug.WriteLine("Maus über Dock ohne Element");
                }
            }
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
                    // Debug.WriteLine($"LogMousePositionAndElements: Button: ID = {dockItem.Id}, DisplayName = {dockItem.DisplayName}, Position = {elementRect.Location}");
                    // Debug.WriteLine($"LogMousePositionAndElements: Button: ID = {dockItem.Id}, DisplayName = {dockItem.DisplayName}, Position = {elementPosition}, Size = {elementRect.Size}");
                    // Debug.WriteLine($"LogMousePositionAndElements: Maus über Button: ID = {dockItem.Id}, DisplayName = {dockItem.DisplayName}");
                    // Debug.WriteLine($"LogMousePositionAndElements: Maus über Button: ID KAT = {mainWindow.isCategoryDockOpenID}");
                    double elementCenterX = elementPosition.X + (elementRect.Width / 2);
                    // Debug.WriteLine($"LogMousePositionAndElements: elementCenterX {elementCenterX}");
                    // double categoryDockPositionX = elementCenterX - (mainWindow.CategoryDockBorder.Width / 2);
                    // double categoryDockBorderWidth = mainWindow.CategoryDockBorder.Width;
                    // Debug.WriteLine($"LogMousePositionAndElements: mainWindow.CategoryDockBorder.Width {mainWindow.CategoryDockBorder.Width}");
                    // double categoryDockPositionX = elementCenterX - (categoryDockBorderWidth / 2);


                    // Debug.WriteLine($"LogMousePositionAndElements: categoryDockPositionX {categoryDockPositionX}");

                    // mainWindow.CategoryDockBorder.Margin = new Thickness(elementCenterX, 0, 0, 0);


                    // Vergewissere dich, dass du die richtige Eigenschaft zum Setzen der Position verwendest.
                    // Hier ein Beispiel, wenn du Canvas verwendest:
                    // Canvas.SetLeft(mainWindow.CategoryDockBorder, categoryDockPositionX);


                    // Oder setze die Margin, falls du sie verwenden möchtest:
                    // mainWindow.CategoryDockBorder.Margin = new Thickness(categoryDockPositionX, mainWindow.CategoryDockBorder.Margin.Top, 0, 0);






                    // Debug.WriteLine($"LogMousePositionAndElements: Element MainWindow Left {mainWindow.Left}");
                    // Debug.WriteLine($"LogMousePositionAndElements: Element MainStackPanel  {mainWindow.MainStackPanel}");
                    // Debug.WriteLine($"LogMousePositionAndElements: Element MainWindow Left {mainWindow.Left}");

                    double mainWindowCenterX = mainWindow.ActualWidth / 2;
                    double positionRelativeToCenter = elementCenterX - mainWindowCenterX;
                    Debug.WriteLine($"LogMousePositionAndElements: Button ID = {dockItem.Id}, DisplayName = {dockItem.DisplayName}, Position relativ zur Mitte des MainWindow = {positionRelativeToCenter}");


                    //  isCategoryDockOpenID
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

                        // Erstelle und übergebe ein StackPanel
                        // StackPanel categoryDock = new StackPanel
                        // {
                        // Name = dockItem.DisplayName,
                        // Children = { new Button { Content = $"Kategorie: {dockItem.DisplayName}", Width = 100, Height = 50 } }
                        // };

                        // mainWindow.ShowCategoryDockPanel(categoryDock);
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


    // private void AnimateButton(Button button)
    // {
    //     var scaleTransform = new ScaleTransform(1.0, 1.0);
    //     button.RenderTransformOrigin = new Point(0.5, 0.5);
    //     button.RenderTransform = scaleTransform;

    //     var scaleXAnimation = new DoubleAnimation
    //     {
    //         From = 1.0,
    //         To = 1.2,
    //         Duration = new Duration(TimeSpan.FromSeconds(0.3)),
    //         AutoReverse = true,
    //         RepeatBehavior = new RepeatBehavior(2)  // Animation 2-mal abspielen
    //     };

    //     var scaleYAnimation = new DoubleAnimation
    //     {
    //         From = 1.0,
    //         To = 1.2,
    //         Duration = new Duration(TimeSpan.FromSeconds(0.3)),
    //         AutoReverse = true,
    //         RepeatBehavior = new RepeatBehavior(2)  // Animation 2-mal abspielen
    //     };

    //     scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
    //     scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
    // }




    // private void StopAnimation(Button button)
    // {
    //     var scaleTransform = button.RenderTransform as ScaleTransform;
    //     if (scaleTransform != null)
    //     {
    //         scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
    //         scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
    //     }
    // }


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
            AddDockItemAt(explorerItem, 0, explorerItem.Category); // currentCategory übergeben

            var cmdItem = new DockItem
            {
                FilePath = @"C:\Windows\System32\cmd.exe",
                DisplayName = "Command Prompt",
                Category = "",
                IsCategory = false,
                Position = 1,
                IconSource = ""
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
        // Aktuellen Stand der Dock-Settings einlesen
        var existingItems = SettingsManager.LoadSettings();

        // Überprüfen, ob die Kategorie bereits existiert
        // var existingCategoryItem = existingItems.FirstOrDefault(item => item.DisplayName == categoryName && item.IsCategory);

        // if (existingCategoryItem != null)
        // {
        //     MessageBox.Show($"Kategorie '{categoryName}' existiert bereits.", "Kategorie existiert", MessageBoxButton.OK, MessageBoxImage.Warning);
        //     return; // Kategorie nicht erneut erstellen
        // }

        // Neue Kategorie erstellen
        var categoryItem = new DockItem
        {
            Id = Guid.NewGuid().ToString(),
            FilePath = "",
            DisplayName = categoryName,
            Category = "",
            IsCategory = true,
            IconSource = "", // IconSource bleibt leer beim ersten Anlegen
        };
        AddDockItemAt(categoryItem, dockPanel.Children.Count, categoryItem.DisplayName);

        // Erstelle und füge ein neues "cmd"-Element zur neuen Kategorie hinzu
        var cmdItem = new DockItem
        {
            Id = Guid.NewGuid().ToString(), // Neue eindeutige ID für das "Command Prompt"-Element
            FilePath = @"C:\Windows\System32\cmd.exe",
            DisplayName = "Command Prompt",
            Category = categoryItem.Id, // Setze die neue Kategorie
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
        Debug.WriteLine("DockPanel_Drop: Drop-Vorgang gestartet");

        if (isDropInProgress)
        {
            Debug.WriteLine("DockPanel_Drop: Drop bereits in Bearbeitung, Vorgang abgebrochen");
            return; // Doppelte Drop-Verhinderung
        }

        try
        {
            if (e.Data.GetDataPresent(DataFormats.Serializable))
            {
                Debug.WriteLine("DockPanel_Drop: Serializable Daten gefunden");

                Button? droppedButton = e.Data.GetData(DataFormats.Serializable) as Button;
                if (droppedButton != null && droppedButton.Tag is DockItem droppedItem)
                {
                    Debug.WriteLine("DockPanel_Drop: Button gefunden und als DockItem erkannt");

                    // Überprüfung auf Kategorie
                    if (!string.IsNullOrEmpty(droppedItem.Category))
                    {
                        Debug.WriteLine($"DockPanel_Drop: Element hat eine Kategorie: {droppedItem.Category}");
                        droppedItem.Category = ""; // Setze die Kategorie auf leer
                    }

                    // Element vom Elternteil trennen
                    var parent = VisualTreeHelper.GetParent(droppedButton) as Panel;
                    if (parent != null)
                    {
                        Debug.WriteLine("DockPanel_Drop: Entferne Button vom Elternpanel");
                        parent.Children.Remove(droppedButton);
                    }

                    Point dropPosition = e.GetPosition(dockPanel);
                    double dropCenterX = dropPosition.X;
                    Debug.WriteLine($"DockPanel_Drop: Drop-Position X: {dropPosition.X}, Y: {dropPosition.Y}");

                    int newIndex = 0;
                    bool inserted = false;
                    for (int i = 0; i < dockPanel.Children.Count; i++)
                    {
                        if (dockPanel.Children[i] is Button button)
                        {
                            Point elementPosition = button.TranslatePoint(new Point(0, 0), dockPanel);
                            double elementCenterX = elementPosition.X + (button.ActualWidth / 2);
                            Debug.WriteLine($"DockPanel_Drop: Button an Position {i} mit CenterX: {elementCenterX}");

                            if (dropCenterX < elementCenterX)
                            {
                                Debug.WriteLine($"DockPanel_Drop: Button wird vor Element {i} eingefügt");
                                dockPanel.Children.Insert(i, droppedButton);
                                inserted = true;
                                break;
                            }
                        }
                        newIndex++;
                    }

                    if (!inserted)
                    {
                        Debug.WriteLine("DockPanel_Drop: Button wird am Ende eingefügt");
                        dockPanel.Children.Add(droppedButton);
                    }

                    // Dock-Items aktualisieren und speichern
                    SaveDockItems(droppedItem.Category);
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Debug.WriteLine("DockPanel_Drop: FileDrop-Daten gefunden");

                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    Debug.WriteLine($"DockPanel_Drop: Datei gefunden: {file}");
                    var dockItem = new DockItem
                    {
                        FilePath = file ?? string.Empty,
                        DisplayName = System.IO.Path.GetFileNameWithoutExtension(file) ?? string.Empty,
                    };
                    Point dropPosition = e.GetPosition(dockPanel);
                    double dropCenterX = dropPosition.X;
                    Debug.WriteLine($"DockPanel_Drop: Drop-Position X: {dropPosition.X}, Y: {dropPosition.Y}");

                    int newIndex = 0;
                    bool inserted = false;
                    for (int i = 0; i < dockPanel.Children.Count; i++)
                    {
                        if (dockPanel.Children[i] is Button button)
                        {
                            Point elementPosition = button.TranslatePoint(new Point(0, 0), dockPanel);
                            double elementCenterX = elementPosition.X + (button.ActualWidth / 2);
                            Debug.WriteLine($"DockPanel_Drop: Button an Position {i} mit CenterX: {elementCenterX}");

                            if (dropCenterX < elementCenterX)
                            {
                                Debug.WriteLine($"DockPanel_Drop: Datei wird vor Element {i} eingefügt");
                                AddDockItemAt(dockItem, i, dockItem.Category);
                                inserted = true;
                                break;
                            }
                        }
                        newIndex++;
                    }
                    if (!inserted)
                    {
                        Debug.WriteLine("DockPanel_Drop: Datei wird am Ende eingefügt");
                        AddDockItemAt(dockItem, newIndex, dockItem.Category);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DockPanel_Drop: Fehler aufgetreten - {ex.Message}");
        }
        finally
        {
            isDropInProgress = false;

            // Entferne alle Platzhalter
            for (int i = 0; i < dockPanel.Children.Count; i++)
            {
                if (dockPanel.Children[i] is Border border && border.Tag as string == "Placeholder")
                {
                    Debug.WriteLine($"DockPanel_Drop: Entferne Platzhalter Border bei Index {i}");
                    dockPanel.Children.Remove(border);
                    i--; // Index anpassen, da ein Element entfernt wurde
                }
            }

            // Setze Hintergrundfarbe zurück
            SolidColorBrush? primaryColor = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
            if (primaryColor != null)
            {
                dockPanel.Background = primaryColor;
            }

            mainWindow.currentDockStatus &= ~MainWindow.DockStatus.DraggingToDock;
            mainWindow.currentDockStatus |= MainWindow.DockStatus.MainDockHover;
            mainWindow.CheckAllConditions();
            mainWindow.SetDragging(false);

            Debug.WriteLine("DockPanel_Drop: Drop-Vorgang abgeschlossen");
            // LoadDockItems();
            ListAllDockPanelElements(); // Auflisten aller Elemente im DockPanel
            mainWindow.HideCategoryDockPanel();

            // Entferne alle Platzhalter und aktualisiere die UI
            for (int i = 0; i < dockPanel.Children.Count; i++)
            {
                if (dockPanel.Children[i] is Border border && border.Tag as string == "Placeholder")
                {
                    Debug.WriteLine($"DockPanel_Drop: Entferne Platzhalter Border bei Index {i}");
                    dockPanel.Children.Remove(border);
                    i--; // Index anpassen, da ein Element entfernt wurde
                }
            }

            // Aktualisiere die UI
            dockPanel.InvalidateVisual();
            dockPanel.UpdateLayout();
        }
    }



    // Methode zum Auflisten aller Elemente im DockPanel
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

        // Debug.WriteLine($"Alle Kinder der Kategorie '{categoryName}' wurden entfernt und gespeichert."); // Debug-Ausgabe
    }



    public void AddDockItemAt(DockItem item, int index, string currentCategory)
    {
        // Check if the item is a category and has an IconSource
        // var iconSource = item.IsCategory && !string.IsNullOrEmpty(item.IconSource) ? item.IconSource : item.FilePath;
        var iconSource = !string.IsNullOrEmpty(item.IconSource) ? item.IconSource : item.FilePath;
        var icon = IconHelper.GetIcon(item.FilePath, iconSource);


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
            Width = 70,
            ToolTip = new ToolTip
            {
                Content = item.DisplayName,
                Placement = PlacementMode.Center,
                HorizontalOffset = 0,
                VerticalOffset = 55
            }
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
                    mainWindow.ShowCategoryDockPanel(new StackPanel
                    {
                        Children = { new Button { Content = $"Kategorie: {dockItem.DisplayName}", Width = 100, Height = 50 } }
                    });
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