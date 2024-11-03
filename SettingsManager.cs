using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public static class SettingsManager
{
    private static string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BiMaDock");
    private static string settingsFilePath = Path.Combine(appDataPath, "docksettings.json");

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






}
