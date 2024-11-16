using BiMaDock;
using System;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Collections;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Threading; // Für den DispatcherTimer



public class SettingsManager
{
    private MainWindow mainWindow;
    private static string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BiMaDock");
    private static string settingsFilePath = Path.Combine(appDataPath, "docksettings.json");

    public SettingsManager(MainWindow mainWindow)
    {
        this.mainWindow = mainWindow;
    }




    public static void SaveSettings(List<DockItem> items)
    {
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }
        var json = JsonConvert.SerializeObject(items, Formatting.Indented);
        File.WriteAllText(settingsFilePath, json);
    }


    public static List<DockItem> LoadSettings()
    {
        if (File.Exists(settingsFilePath))
        {
            var json = File.ReadAllText(settingsFilePath);
            var items = JsonConvert.DeserializeObject<List<DockItem>>(json);
            return items ?? new List<DockItem>();
        }
        return new List<DockItem>();
    }



    public static void SetColors(MainWindow mainWindow)
    {
        Console.WriteLine("SetColors: Aufruf der Methode");

        // Greife auf die aktuellen Ressourcen zu
        var primaryColor = Application.Current.Resources["PrimaryColor"];
        Console.WriteLine($"Test_Click: primaryColor {primaryColor}");

        // Ressourcen zur Laufzeit ändern
        Application.Current.Resources["PrimaryColor"] = new SolidColorBrush(Color.FromRgb(255, 0, 0)); // Rot
        Application.Current.Resources["SecondaryColor"] = new SolidColorBrush(Color.FromRgb(255, 0, 0)); // Rot
        mainWindow.DockPanel.Background = new SolidColorBrush(Color.FromRgb(255, 0, 0)); // Rot
        
        

    }






}
