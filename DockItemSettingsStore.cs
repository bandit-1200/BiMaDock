using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace BiMaDock;

internal static class DockItemSettingsStore
{
    public static List<DockItem> Load(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<DockItem>>(json) ?? new List<DockItem>();
        }
        catch (JsonException exception)
        {
            var backupPath = $"{filePath}.corrupt-{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture)}";
            File.Move(filePath, backupPath);
            Trace.TraceError($"Dock settings at '{filePath}' were invalid JSON and were preserved at '{backupPath}': {exception}");
            return new List<DockItem>();
        }
    }

    public static void Save(string filePath, string json)
    {
        var temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, filePath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
