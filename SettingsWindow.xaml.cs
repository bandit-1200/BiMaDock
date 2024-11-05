using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;
using System.IO;

namespace BiMaDock
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            ShowVersionInConsole();
        }

        private void ShowVersionInConsole()
        {
            // InformationalVersion abrufen
            var informationalVersionAttribute = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                
            string informationalVersion = informationalVersionAttribute != null 
                ? informationalVersionAttribute.InformationalVersion 
                : "Unbekannte Version";  // Fallback, wenn null

            string clearVersion = informationalVersion.Split('+')[0];  // Unabhängig von null

            System.Console.WriteLine($"Detaillierte Version: {informationalVersion}");
            System.Console.WriteLine($"Klare Version: {clearVersion}");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var primaryColor = PrimaryColorPicker.SelectedColor ?? Colors.Transparent;
            var secondaryColor = SecondaryColorPicker.SelectedColor ?? Colors.Transparent;

            var settings = new
            {
                PrimaryColor = primaryColor.ToString(),
                SecondaryColor = secondaryColor.ToString(),
                AnimationSpeed = AnimationSpeedSlider?.Value ?? 1.0
            };

            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string directoryPath = Path.Combine(appDataPath, "BiMaDock");

            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string filePath = Path.Combine(directoryPath, "StyleSettings.json");
            File.WriteAllText(filePath, json);
            MessageBox.Show("Einstellungen gespeichert!");
        }

        private void PrimaryColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
            {
                var color = e.NewValue.Value;
                PrimaryColorPicker.Background = new SolidColorBrush(color);
            }
        }

        private void SecondaryColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
            {
                var color = e.NewValue.Value;
                SecondaryColorPicker.Background = new SolidColorBrush(color);
            }
        }

        private void ShowVersionButton_Click(object sender, RoutedEventArgs e)
        {
            var informationalVersionAttribute = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            string informationalVersion = informationalVersionAttribute != null 
                ? informationalVersionAttribute.InformationalVersion 
                : "Unbekannte Version";  // Fallback, wenn null

            string clearVersion = informationalVersion.Split('+')[0];
            MessageBox.Show($"Detaillierte Version: {informationalVersion}\nKlare Version: {clearVersion}");
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
