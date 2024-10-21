using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using MyDockApp;

public class DockManager
{
    private StackPanel dockPanel;
    private MainWindow mainWindow;

    public DockManager(StackPanel panel, MainWindow window)
    {
        dockPanel = panel;
        mainWindow = window;
        dockPanel.Drop += DockPanel_Drop;
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
            if (button.Tag is string filePath)
            {
                items.Add(new DockItem
                {
                    FilePath = filePath,
                    DisplayName = System.IO.Path.GetFileNameWithoutExtension(filePath) ?? string.Empty,
                    // Kategorie später hinzufügen
                });
            }
        }
        SettingsManager.SaveSettings(items);
    }

    private void DockPanel_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
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
    button.Click += (s, args) =>
    {
        Console.WriteLine("Element geklickt: " + item.FilePath); // Debug-Ausgabe
        mainWindow.OpenFile(item.FilePath);
    };

    // Kontextmenü für die Schaltfläche
    button.MouseRightButtonDown += (s, e) =>
    {
        Console.WriteLine("Rechtsklick auf Element: " + item.DisplayName); // Debug-Ausgabe
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
