using BiMaDock;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;


public class SettingsManager
{
    private static string settingsFilePath = AppPaths.GetSettingsFilePath("docksettings.json");
    private static List<DockItem>? cachedItems;

    public static void SaveSettings(List<DockItem> items)
    {
        AppPaths.EnsureAppDataDirectory();
        var json = JsonConvert.SerializeObject(items, Formatting.Indented);
        DockItemSettingsStore.Save(settingsFilePath, json);
        cachedItems = items;
    }


    public static List<DockItem> LoadSettings()
    {
        if (cachedItems != null)
        {
            return cachedItems;
        }

        if (File.Exists(settingsFilePath))
        {
            cachedItems = DockItemSettingsStore.Load(settingsFilePath);
            return cachedItems;
        }
        cachedItems = new List<DockItem>();
        return cachedItems;
    }

}
