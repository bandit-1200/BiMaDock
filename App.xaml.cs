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
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BiMaDock",
            "startup.log");

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

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                Log("OnStartup begin");
                bool createdNew;
                singleInstanceMutex = new Mutex(true, "BiMaDock_SingleInstance", out createdNew);
                Log($"Single instance mutex created: {createdNew}");

                if (!createdNew)
                {
                    Log("Another instance is already running. Exiting.");
                    MessageBox.Show("BiMaDock läuft bereits. Bitte schließen Sie die laufende Instanz zuerst.", "BiMaDock", MessageBoxButton.OK, MessageBoxImage.Information);
                    Shutdown();
                    return;
                }

                base.OnStartup(e);
                Log("base.OnStartup completed");

                foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
                {
                    Log("ResourceDictionary loaded");
                    foreach (var key in dictionary.Keys)
                    {
                        Log("Resource key: " + key);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("OnStartup exception: " + ex);
                MessageBox.Show("Beim Starten von BiMaDock ist ein Fehler aufgetreten.\n\n" + ex.Message, "BiMaDock Startfehler", MessageBoxButton.OK, MessageBoxImage.Error);
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
