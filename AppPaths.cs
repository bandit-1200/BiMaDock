using System;
using System.IO;

namespace BiMaDock
{
    internal static class AppPaths
    {
        /// <summary>
        /// Optionaler Datenordner, z. B. für UI-Tests. Ohne Angabe wird %LOCALAPPDATA%\BiMaDock verwendet.
        /// </summary>
        public const string DataDirectoryVariable = "BIMADOCK_DATA_DIR";

        public static string AppDataDirectory
        {
            get
            {
                string? overrideDirectory = Environment.GetEnvironmentVariable(DataDirectoryVariable);
                return string.IsNullOrWhiteSpace(overrideDirectory)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BiMaDock")
                    : Path.GetFullPath(overrideDirectory);
            }
        }

        public static bool IsDataDirectoryOverridden =>
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(DataDirectoryVariable));

        public static string EnsureAppDataDirectory()
        {
            Directory.CreateDirectory(AppDataDirectory);
            return AppDataDirectory;
        }

        public static string GetLogFilePath() => Path.Combine(AppDataDirectory, "startup.log");

        public static string GetIconsDirectory() => Path.Combine(AppDataDirectory, "Icons");

        public static string GetSettingsFilePath(string fileName = "StyleSettings.json") =>
            Path.Combine(EnsureAppDataDirectory(), fileName);

        public static string GetUpdateConfigFilePath() => Path.Combine(AppDataDirectory, "update_config.txt");
    }
}
