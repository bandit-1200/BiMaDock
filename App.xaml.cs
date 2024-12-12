using System;
using System.Windows;
using System.Diagnostics;

namespace BiMaDock
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
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
