using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Diagnostics;

namespace BiMaDock
{
    public partial class App : Application
    {
        private static Mutex? singleInstanceMutex;
        private static readonly string LogFilePath = AppPaths.GetLogFilePath();

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                Log("UnhandledException: " + args.ExceptionObject);
            };

            DispatcherUnhandledException += (_, args) =>
            {
                Log("DispatcherUnhandledException: " + args.Exception);
                args.Handled = false;
            };
        }

        private static void Log(string message)
        {
            try
            {
                var directory = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch
            {
                // Intentionally ignored to avoid a logging crash.
            }
        }

        // Mit eigenem Datenordner (z. B. UI-Tests) darf parallel zur normalen Instanz gestartet werden.
        private static string GetSingleInstanceMutexName()
        {
            const string baseName = "BiMaDock_SingleInstance";
            if (!AppPaths.IsDataDirectoryOverridden)
            {
                return baseName;
            }

            byte[] hash = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(AppPaths.AppDataDirectory.ToUpperInvariant()));
            return $"{baseName}_{Convert.ToHexString(hash, 0, 8)}";
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                Log("OnStartup begin");
                bool createdNew;
                singleInstanceMutex = new Mutex(true, GetSingleInstanceMutexName(), out createdNew);
                Log($"Single instance mutex created: {createdNew}");

                if (!createdNew)
                {
                    Log("Another instance is already running. Exiting.");
                    DialogMessageBox.Show("BiMaDock läuft bereits. Bitte schließen Sie die laufende Instanz zuerst.", "BiMaDock", MessageBoxButton.OK, MessageBoxImage.Information);
                    Shutdown();
                    return;
                }

                AppPaths.EnsureAppDataDirectory();

                base.OnStartup(e);
                Log("base.OnStartup completed");

                bool shouldLogResources = Debugger.IsAttached || string.Equals(
                    Environment.GetEnvironmentVariable("BIMADOCK_DEBUG_LOGGING"),
                    "1",
                    StringComparison.OrdinalIgnoreCase);

                if (shouldLogResources && Application.Current?.Resources != null)
                {
                    foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
                    {
                        Log("ResourceDictionary loaded");
                        foreach (var key in dictionary.Keys)
                        {
                            Log("Resource key: " + key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("OnStartup exception: " + ex);
                DialogMessageBox.Show("Beim Starten von BiMaDock ist ein Fehler aufgetreten.\n\n" + ex.Message, "BiMaDock Startfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                Log("OnExit begin");
                singleInstanceMutex?.ReleaseMutex();
                singleInstanceMutex?.Dispose();
            }
            catch (Exception ex)
            {
                Log("OnExit exception: " + ex);
            }

            base.OnExit(e);
        }
    }
}
