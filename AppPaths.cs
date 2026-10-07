using System;
using System.IO;

namespace BiMaDock
{
    internal static class AppPaths
    {
        public static string AppDataDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BiMaDock");

        public static string EnsureAppDataDirectory()
        {
            Directory.CreateDirectory(AppDataDirectory);
            return AppDataDirectory;
        }

        public static string GetLogFilePath() => Path.Combine(AppDataDirectory, "startup.log");

        public static string GetSettingsFilePath(string fileName = "StyleSettings.json") =>
            Path.Combine(EnsureAppDataDirectory(), fileName);

        public static string GetUpdateConfigFilePath() => Path.Combine(AppDataDirectory, "update_config.txt");
    }
}
