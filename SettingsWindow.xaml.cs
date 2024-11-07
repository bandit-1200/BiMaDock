using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;
using System.IO;

namespace BiMaDock
{
    public partial class SettingsWindow : Window
    {
        private readonly string settingsFilePath;

        public SettingsWindow()
        {
            InitializeComponent();
            settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BiMaDock", "StyleSettings.json");

            ShowVersionInConsole();
            AutoStartCheckBox.IsChecked = StartupManager.IsInStartup();
            LoadSettings();
        }

        private void ShowVersionInConsole()
        {
            var informationalVersionAttribute = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            string informationalVersion = informationalVersionAttribute?.InformationalVersion ?? "Unbekannte Version";
            string clearVersion = informationalVersion.Split('+')[0];

            System.Console.WriteLine($"Detaillierte Version: {informationalVersion}");
            System.Console.WriteLine($"Klare Version: {clearVersion}");
        }

        private void LoadSettings()
        {
            if (File.Exists(settingsFilePath))
            {
                string json = File.ReadAllText(settingsFilePath);
                dynamic settings = JsonConvert.DeserializeObject(json);

                if (settings != null)
                {
                    if (ColorConverter.ConvertFromString((string)settings.PrimaryColor) is Color primaryColor)
                    {
                        PrimaryColorPicker.SelectedColor = primaryColor;
                        PrimaryColorPreview.Background = new SolidColorBrush(primaryColor);
                    }

                    if (ColorConverter.ConvertFromString((string)settings.SecondaryColor) is Color secondaryColor)
                    {
                        SecondaryColorPicker.SelectedColor = secondaryColor;
                        SecondaryColorPreview.Background = new SolidColorBrush(secondaryColor);
                    }

                    AnimationSpeedSlider.Value = settings.AnimationSpeed;
                }
            }
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

            Directory.CreateDirectory(directoryPath);
            File.WriteAllText(Path.Combine(directoryPath, "StyleSettings.json"), json);
            MessageBox.Show("Einstellungen gespeichert!");
        }

        private void PrimaryColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
            {
                PrimaryColorPicker.Background = new SolidColorBrush(e.NewValue.Value);
                PrimaryColorPreview.Background = new SolidColorBrush(e.NewValue.Value);
            }
        }

        private void SecondaryColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
            {
                SecondaryColorPicker.Background = new SolidColorBrush(e.NewValue.Value);
                SecondaryColorPreview.Background = new SolidColorBrush(e.NewValue.Value);
            }
        }

        private void ShowVersionButton_Click(object sender, RoutedEventArgs e)
        {
            var informationalVersionAttribute = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            string informationalVersion = informationalVersionAttribute?.InformationalVersion ?? "Unbekannte Version";
            string clearVersion = informationalVersion.Split('+')[0];

            MessageBox.Show($"Detaillierte Version: {informationalVersion}\nKlare Version: {clearVersion}");
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AutoStartCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            StartupManager.AddToStartup(true);
        }

        private void AutoStartCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            StartupManager.AddToStartup(false);
        }
    }
}
