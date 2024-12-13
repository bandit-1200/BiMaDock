using System;
using System.Threading;
using System.Windows;
using System.Diagnostics;

namespace BiMaDock
{
    public partial class App : Application
    {
        private static Mutex mutex = new Mutex(true, "{UniqueAppID}");

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                // Beenden Sie die Anwendung ohne eine Warnung anzuzeigen
                Environment.Exit(0);
            }

            base.OnStartup(e);

            // Debug-Ausgabe: Auflistung aller geladenen ResourceDictionaries
            Debug.WriteLine("App: Auflistung aller geladenen ResourceDictionaries...");
            foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
            {
                Debug.WriteLine("App: ResourceDictionary gefunden.");
                foreach (var key in dictionary.Keys)
                {
                    Debug.WriteLine("App: Schlüssel gefunden: " + key);
                }
            }
        }
    }
}
