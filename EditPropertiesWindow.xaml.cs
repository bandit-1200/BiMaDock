using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;



namespace MyDockApp
{
    public partial class EditPropertiesWindow : Window
    {
        private const string Icons8ApiKey = "YOUR_ICONS8_API_KEY";
        private const string Icons8ApiUrl = "https://api.icons8.com/api/iconsets/v5/search-icons";

        public EditPropertiesWindow()
        {
            InitializeComponent();
            _ = LoadIconsAsync();
        }

        private async Task LoadIconsAsync()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await client.GetAsync($"{Icons8ApiUrl}?api_key={Icons8ApiKey}&term=windows");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine("API Response: " + jsonResponse); // Debug-Ausgabe der API-Antwort

            var icons = JsonConvert.DeserializeObject<Icons8Response>(jsonResponse);
            if (icons != null && icons.Icons != null)
            {
                foreach (var icon in icons.Icons)
                {
                    Console.WriteLine("Icon URL: " + icon.Url); // Debug-Ausgabe der Icon-URLs
                    var image = new Image
                    {
                        Source = new BitmapImage(new Uri(icon.Url)),
                        Width = 32,
                        Height = 32,
                        Margin = new Thickness(5)
                    };
                    SymbolPanel.Children.Add(image);
                }
            }
            else
            {
                Console.WriteLine("Keine Icons gefunden."); // Debug-Ausgabe, falls keine Icons gefunden wurden
            }
        }


        private class Icons8Response
        {
            public List<Icon>? Icons { get; set; }

            public class Icon
            {
                public string Url { get; set; } = string.Empty;
            }
        }



        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Speichern der Änderungen
            this.DialogResult = true; // Schließen des Fensters mit Erfolg
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Abbrechen der Änderungen
            this.DialogResult = false; // Schließen des Fensters ohne Erfolg
        }
    }
}
