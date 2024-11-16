using System;
using System.Windows;

namespace BiMaDock
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Debug-Ausgabe: Auflistung aller geladenen ResourceDictionaries
            Console.WriteLine("App: Auflistung aller geladenen ResourceDictionaries...");
            foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
            {
                Console.WriteLine("App: ResourceDictionary gefunden.");
                foreach (var key in dictionary.Keys)
                {
                    Console.WriteLine("App: Schlüssel gefunden: " + key);
                }
            }
        }
    }
}
