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
    private static string appDataPath = AppPaths.AppDataDirectory;
    private static string settingsFilePath = AppPaths.GetSettingsFilePath("docksettings.json");
    private static List<DockItem>? cachedItems;

    public SettingsManager(MainWindow mainWindow)
    {
        this.mainWindow = mainWindow;
    }




    public static void SaveSettings(List<DockItem> items)
    {
        AppPaths.EnsureAppDataDirectory();
        cachedItems = items;
        var json = JsonConvert.SerializeObject(cachedItems, Formatting.Indented);
        File.WriteAllText(settingsFilePath, json);
    }


    public static List<DockItem> LoadSettings()
    {
        if (cachedItems != null)
        {
            return cachedItems;
        }

        if (File.Exists(settingsFilePath))
        {
            var json = File.ReadAllText(settingsFilePath);
            cachedItems = JsonConvert.DeserializeObject<List<DockItem>>(json) ?? new List<DockItem>();
            return cachedItems;
        }
        cachedItems = new List<DockItem>();
        return cachedItems;
    }



    public static void SetColors(MainWindow mainWindow)
    {
        Debug.WriteLine("SetColors: Aufruf der Methode");

        // Greife auf die aktuellen Ressourcen zu
        // var primaryColor = Application.Current.Resources["PrimaryColor"];
        // Debug.WriteLine($"Test_Click: primaryColor {primaryColor}");

        // Ressourcen zur Laufzeit ändern TEST
        // Application.Current.Resources["PrimaryColor"] = new SolidColorBrush(Color.FromRgb(255, 0, 0)); // Rot
        // Application.Current.Resources["SecondaryColor"] = new SolidColorBrush(Color.FromRgb(255, 0, 0)); // Rot
        // mainWindow.DockPanel.Background = new SolidColorBrush(Color.FromRgb(255, 0, 0)); // Rot
        
        

    }






}
