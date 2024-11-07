using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;
using System.IO;
using System.Windows.Controls;



namespace BiMaDock
{
    public partial class SettingsWindow : Window
    {
        private readonly string settingsFilePath;

        public class ScaleSettings
        {
            public double Duration { get; set; }
            public double ScaleFactor { get; set; }
            public bool AutoReverse { get; set; }
            public int EffectIndex { get; set; }
        }


        public class RotateSettings
        {
            public double Duration { get; set; }
            public double Angle { get; set; }
            public bool AutoReverse { get; set; }
            public int EffectIndex { get; set; }
        }

        public class TranslateSettings
        {
            public double Duration { get; set; }
            public double TranslateX { get; set; }
            public double TranslateY { get; set; }
            public bool AutoReverse { get; set; }
            public int EffectIndex { get; set; }
        }
        private ComboBox? animationEffectComboBox;
        private Slider? durationSlider;
        private Slider? scaleFactorSlider;
        private Slider? angleSlider;
        private Slider? translateXSlider;
        private Slider? translateYSlider;
        private CheckBox? autoReverseCheckBox;





        public SettingsWindow()
        {
            InitializeComponent();
            CreateAnimationEffectDropdown();
            // animationEffectComboBox = new ComboBox();
            settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BiMaDock", "StyleSettings.json");

            ShowVersionInConsole();
            AutoStartCheckBox.IsChecked = StartupManager.IsInStartup();
            LoadSettings();
        }



        private void CreateAnimationEffectDropdown()
        {
            // Initialisiere und weise die ComboBox dem globalen Feld zu
            animationEffectComboBox = new ComboBox
            {
                Name = "AnimationEffectComboBox",
                Margin = new Thickness(0, 0, 0, 20)
            };

            animationEffectComboBox.Items.Add("Kein Effekt");
            animationEffectComboBox.Items.Add("Scale");
            animationEffectComboBox.Items.Add("Rotate");
            animationEffectComboBox.Items.Add("Translate");
            animationEffectComboBox.SelectedIndex = 0;  // Setze den initialen Index auf "Kein Effekt"
            animationEffectComboBox.SelectionChanged += AnimationEffectComboBox_SelectionChanged;
            AnimationSettingsPanel.Children.Add(animationEffectComboBox);

            // Initiale Einstellungen erstellen (z.B. Scale)
            CreateScaleAnimationSettings();
        }



        private void AnimationEffectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                // Überprüfe, ob SelectedItem nicht null ist
                string? selectedEffect = comboBox.SelectedItem?.ToString();

                if (!string.IsNullOrEmpty(selectedEffect))
                {
                    // Entferne nur die dynamisch erstellten Kinder, nicht das Dropdown-Menü
                    while (AnimationSettingsPanel.Children.Count > 2) // Da das Dropdown-Menü das zweite Element ist
                    {
                        AnimationSettingsPanel.Children.RemoveAt(2);
                    }

                    // Logik zur Erstellung der Animationseinstellungen basierend auf der Auswahl
                    if (selectedEffect == "Scale")
                    {
                        CreateScaleAnimationSettings();
                    }
                    else if (selectedEffect == "Rotate")
                    {
                        CreateRotateAnimationSettings();
                    }
                    else if (selectedEffect == "Translate")
                    {
                        CreateTranslateAnimationSettings();
                    }

                    // Ausgabe des aktuellen SelectedIndex auf der Konsole
                    Console.WriteLine($"SelectedIndex: {comboBox.SelectedIndex}");
                }
            }
        }


        private void CreateScaleAnimationSettings()
        {
            // Slider für die Animationsdauer
            TextBlock durationTextBlock = new TextBlock
            {
                Text = "Dauer (in Sekunden):",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            AnimationSettingsPanel.Children.Add(durationTextBlock);

            // Initialisiere und speichere die Referenz auf den globalen Slider für die Dauer
            durationSlider = new Slider
            {
                Minimum = 0.1,
                Maximum = 3.0,
                Value = 0.3,
                TickFrequency = 0.1,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(durationSlider);

            // TextBlock zur Anzeige des aktuellen Werts des Sliders
            TextBlock durationValueTextBlock = new TextBlock
            {
                Text = $"Aktuelle Dauer: {durationSlider.Value} s",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(durationValueTextBlock);

            // Event-Handler zur Aktualisierung des TextBlocks bei Änderung des Slider-Werts
            durationSlider.ValueChanged += (s, e) =>
            {
                durationValueTextBlock.Text = $"Aktuelle Dauer: {durationSlider.Value} s";
            };

            // Slider für den Skalierungsfaktor
            TextBlock scaleFactorTextBlock = new TextBlock
            {
                Text = "Skalierungsfaktor:",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            AnimationSettingsPanel.Children.Add(scaleFactorTextBlock);

            // Initialisiere und speichere die Referenz auf den globalen Slider für den Skalierungsfaktor
            scaleFactorSlider = new Slider
            {
                Minimum = 1.0,
                Maximum = 2.0,
                Value = 1.2,
                TickFrequency = 0.1,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(scaleFactorSlider);

            // TextBlock zur Anzeige des aktuellen Werts des Sliders
            TextBlock scaleFactorValueTextBlock = new TextBlock
            {
                Text = $"Aktueller Skalierungsfaktor: {scaleFactorSlider.Value}",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(scaleFactorValueTextBlock);

            // Event-Handler zur Aktualisierung des TextBlocks bei Änderung des Slider-Werts
            scaleFactorSlider.ValueChanged += (s, e) =>
            {
                scaleFactorValueTextBlock.Text = $"Aktueller Skalierungsfaktor: {scaleFactorSlider.Value}";
            };

            // Initialisiere und speichere die Referenz auf die globale CheckBox für AutoReverse
            autoReverseCheckBox = new CheckBox
            {
                Content = "Automatisches Rückwärtslaufen (AutoReverse)",
                Foreground = Brushes.White,
                IsChecked = true,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(autoReverseCheckBox);
        }


        private void CreateRotateAnimationSettings()
        {
            // Slider für die Animationsdauer
            TextBlock durationTextBlock = new TextBlock
            {
                Text = "Dauer (in Sekunden):",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            AnimationSettingsPanel.Children.Add(durationTextBlock);

            // Initialisiere und speichere die Referenz auf den globalen Slider für die Dauer
            durationSlider = new Slider
            {
                Minimum = 0.1,
                Maximum = 3.0,
                Value = 0.3,
                TickFrequency = 0.1,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(durationSlider);

            // TextBlock zur Anzeige des aktuellen Werts des Sliders
            TextBlock durationValueTextBlock = new TextBlock
            {
                Text = $"Aktuelle Dauer: {durationSlider.Value} s",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(durationValueTextBlock);

            // Event-Handler zur Aktualisierung des TextBlocks bei Änderung des Slider-Werts
            durationSlider.ValueChanged += (s, e) =>
            {
                durationValueTextBlock.Text = $"Aktuelle Dauer: {durationSlider.Value} s";
            };

            // Slider für den Rotationswinkel
            TextBlock angleTextBlock = new TextBlock
            {
                Text = "Rotationswinkel (in Grad):",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            AnimationSettingsPanel.Children.Add(angleTextBlock);

            // Initialisiere und speichere die Referenz auf den globalen Slider für den Winkel
            angleSlider = new Slider
            {
                Minimum = 0.0,
                Maximum = 360.0,
                Value = 180.0,
                TickFrequency = 10.0,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(angleSlider);

            // TextBlock zur Anzeige des aktuellen Werts des Sliders
            TextBlock angleValueTextBlock = new TextBlock
            {
                Text = $"Aktueller Winkel: {angleSlider.Value}°",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(angleValueTextBlock);

            // Event-Handler zur Aktualisierung des TextBlocks bei Änderung des Slider-Werts
            angleSlider.ValueChanged += (s, e) =>
            {
                angleValueTextBlock.Text = $"Aktueller Winkel: {angleSlider.Value}°";
            };

            // Initialisiere und speichere die Referenz auf die globale CheckBox für AutoReverse
            autoReverseCheckBox = new CheckBox
            {
                Content = "Automatisches Rückwärtslaufen (AutoReverse)",
                Foreground = Brushes.White,
                IsChecked = true,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(autoReverseCheckBox);
        }

        // {
        //     // Slider für die Animationsdauer
        //     TextBlock durationTextBlock = new TextBlock
        //     {
        //         Text = "Dauer (in Sekunden):",
        //         Foreground = Brushes.White,
        //         Margin = new Thickness(0, 0, 0, 5)
        //     };
        //     AnimationSettingsPanel.Children.Add(durationTextBlock);

        //     Slider durationSlider = new Slider
        //     {
        //         Minimum = 0.1,
        //         Maximum = 3.0,
        //         Value = 0.3,
        //         TickFrequency = 0.1,
        //         IsSnapToTickEnabled = true,
        //         Margin = new Thickness(0, 0, 0, 20)
        //     };
        //     AnimationSettingsPanel.Children.Add(durationSlider);

        //     // TextBlock zur Anzeige des aktuellen Werts des Sliders
        //     TextBlock durationValueTextBlock = new TextBlock
        //     {
        //         Text = $"Aktuelle Dauer: {durationSlider.Value} s",
        //         Foreground = Brushes.White,
        //         Margin = new Thickness(0, 0, 0, 20)
        //     };
        //     AnimationSettingsPanel.Children.Add(durationValueTextBlock);

        //     // Event-Handler zur Aktualisierung des TextBlocks bei Änderung des Slider-Werts
        //     durationSlider.ValueChanged += (s, e) =>
        //     {
        //         durationValueTextBlock.Text = $"Aktuelle Dauer: {durationSlider.Value} s";
        //     };

        //     // Slider für die X-Achsen-Translation
        //     TextBlock translateXTextBlock = new TextBlock
        //     {
        //         Text = "X-Translation (in Pixel):",
        //         Foreground = Brushes.White,
        //         Margin = new Thickness(0, 0, 0, 5)
        //     };
        //     AnimationSettingsPanel.Children.Add(translateXTextBlock);

        //     Slider translateXSlider = new Slider
        //     {
        //         Minimum = -100.0,
        //         Maximum = 100.0,
        //         Value = 0.0,
        //         TickFrequency = 10.0,
        //         IsSnapToTickEnabled = true,
        //         Margin = new Thickness(0, 0, 0, 20)
        //     };
        //     AnimationSettingsPanel.Children.Add(translateXSlider);

        //     // TextBlock zur Anzeige des aktuellen Werts des Sliders
        //     TextBlock translateXValueTextBlock = new TextBlock
        //     {
        //         Text = $"Aktuelle X-Translation: {translateXSlider.Value} px",
        //         Foreground = Brushes.White,
        //         Margin = new Thickness(0, 0, 0, 20)
        //     };
        //     AnimationSettingsPanel.Children.Add(translateXValueTextBlock);

        //     // Event-Handler zur Aktualisierung des TextBlocks bei Änderung des Slider-Werts
        //     translateXSlider.ValueChanged += (s, e) =>
        //     {
        //         translateXValueTextBlock.Text = $"Aktuelle X-Translation: {translateXSlider.Value} px";
        //     };

        //     // Slider für die Y-Achsen-Translation
        //     TextBlock translateYTextBlock = new TextBlock
        //     {
        //         Text = "Y-Translation (in Pixel):",
        //         Foreground = Brushes.White,
        //         Margin = new Thickness(0, 0, 0, 5)
        //     };
        //     AnimationSettingsPanel.Children.Add(translateYTextBlock);

        //     Slider translateYSlider = new Slider
        //     {
        //         Minimum = -100.0,
        //         Maximum = 100.0,
        //         Value = 0.0,
        //         TickFrequency = 10.0,
        //         IsSnapToTickEnabled = true,
        //         Margin = new Thickness(0, 0, 0, 20)
        //     };
        //     AnimationSettingsPanel.Children.Add(translateYSlider);

        //     // TextBlock zur Anzeige des aktuellen Werts des Sliders
        //     TextBlock translateYValueTextBlock = new TextBlock
        //     {
        //         Text = $"Aktuelle Y-Translation: {translateYSlider.Value} px",
        //         Foreground = Brushes.White,
        //         Margin = new Thickness(0, 0, 0, 20)
        //     };
        //     AnimationSettingsPanel.Children.Add(translateYValueTextBlock);

        //     // Event-Handler zur Aktualisierung des TextBlocks bei Änderung des Slider-Werts
        //     translateYSlider.ValueChanged += (s, e) =>
        //     {
        //         translateYValueTextBlock.Text = $"Aktuelle Y-Translation: {translateYSlider.Value} px";
        //     };

        //     // CheckBox für AutoReverse
        //     CheckBox autoReverseCheckBox = new CheckBox
        //     {
        //         Content = "Automatisches Rückwärtslaufen (AutoReverse)",
        //         Foreground = Brushes.White,
        //         IsChecked = true,
        //         Margin = new Thickness(0, 0, 0, 20)
        //     };
        //     AnimationSettingsPanel.Children.Add(autoReverseCheckBox);
        // }
        private void CreateTranslateAnimationSettings()
        {
            // Slider für die Animationsdauer
            TextBlock durationTextBlock = new TextBlock
            {
                Text = "Dauer (in Sekunden):",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            AnimationSettingsPanel.Children.Add(durationTextBlock);

            // Initialisiere und speichere die Referenz auf den globalen Slider für die Dauer
            durationSlider = new Slider
            {
                Minimum = 0.1,
                Maximum = 3.0,
                Value = 0.3,
                TickFrequency = 0.1,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(durationSlider);

            // TextBlock zur Anzeige des aktuellen Werts des Sliders
            TextBlock durationValueTextBlock = new TextBlock
            {
                Text = $"Aktuelle Dauer: {durationSlider.Value} s",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(durationValueTextBlock);

            // Event-Handler zur Aktualisierung des TextBlocks bei Änderung des Slider-Werts
            durationSlider.ValueChanged += (s, e) =>
            {
                durationValueTextBlock.Text = $"Aktuelle Dauer: {durationSlider.Value} s";
            };

            // Slider für die X-Achsen-Translation
            TextBlock translateXTextBlock = new TextBlock
            {
                Text = "X-Translation (in Pixel):",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            AnimationSettingsPanel.Children.Add(translateXTextBlock);

            // Initialisiere und speichere die Referenz auf den globalen Slider für die X-Achsen-Translation
            translateXSlider = new Slider
            {
                Minimum = -100.0,
                Maximum = 100.0,
                Value = 0.0,
                TickFrequency = 10.0,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(translateXSlider);

            // TextBlock zur Anzeige des aktuellen Werts des Sliders
            TextBlock translateXValueTextBlock = new TextBlock
            {
                Text = $"Aktuelle X-Translation: {translateXSlider.Value} px",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(translateXValueTextBlock);

            // Event-Handler zur Aktualisierung des TextBlocks bei Änderung des Slider-Werts
            translateXSlider.ValueChanged += (s, e) =>
            {
                translateXValueTextBlock.Text = $"Aktuelle X-Translation: {translateXSlider.Value} px";
            };

            // Slider für die Y-Achsen-Translation
            TextBlock translateYTextBlock = new TextBlock
            {
                Text = "Y-Translation (in Pixel):",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            AnimationSettingsPanel.Children.Add(translateYTextBlock);

            // Initialisiere und speichere die Referenz auf den globalen Slider für die Y-Achsen-Translation
            translateYSlider = new Slider
            {
                Minimum = -100.0,
                Maximum = 100.0,
                Value = 0.0,
                TickFrequency = 10.0,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(translateYSlider);

            // TextBlock zur Anzeige des aktuellen Werts des Sliders
            TextBlock translateYValueTextBlock = new TextBlock
            {
                Text = $"Aktuelle Y-Translation: {translateYSlider.Value} px",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(translateYValueTextBlock);

            // Event-Handler zur Aktualisierung des TextBlocks bei Änderung des Slider-Werts
            translateYSlider.ValueChanged += (s, e) =>
            {
                translateYValueTextBlock.Text = $"Aktuelle Y-Translation: {translateYSlider.Value} px";
            };

            // Initialisiere und speichere die Referenz auf die globale CheckBox für AutoReverse
            autoReverseCheckBox = new CheckBox
            {
                Content = "Automatisches Rückwärtslaufen (AutoReverse)",
                Foreground = Brushes.White,
                IsChecked = true,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(autoReverseCheckBox);
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
                var settings = JsonConvert.DeserializeObject<dynamic>(json);

                if (settings != null)
                {
                    if (settings.PrimaryColor != null && ColorConverter.ConvertFromString((string)settings.PrimaryColor) is Color primaryColor)
                    {
                        PrimaryColorPicker.SelectedColor = primaryColor;
                        PrimaryColorPreview.Background = new SolidColorBrush(primaryColor);
                    }

                    if (settings.SecondaryColor != null && ColorConverter.ConvertFromString((string)settings.SecondaryColor) is Color secondaryColor)
                    {
                        SecondaryColorPicker.SelectedColor = secondaryColor;
                        SecondaryColorPreview.Background = new SolidColorBrush(secondaryColor);
                    }

                    // if (settings.FadeDuration != null)
                    // {
                    //     FadeDurationSlider.Value = settings.FadeDuration;
                    // }
                }
            }
        }
private void SaveButton_Click(object sender, RoutedEventArgs e)
{
    var primaryColor = PrimaryColorPicker.SelectedColor ?? Colors.Transparent;
    var secondaryColor = SecondaryColorPicker.SelectedColor ?? Colors.Transparent;

    // Standardwerte setzen, falls keine Änderungen vorgenommen wurden
    var scaleSettings = new ScaleSettings
    {
        Duration = durationSlider?.Value ?? 0.3,
        ScaleFactor = scaleFactorSlider?.Value ?? 1.2,
        AutoReverse = autoReverseCheckBox?.IsChecked ?? true,
        EffectIndex = 1
    };

    var rotateSettings = new RotateSettings
    {
        Duration = durationSlider?.Value ?? 0.3,
        Angle = angleSlider?.Value ?? 180.0,
        AutoReverse = autoReverseCheckBox?.IsChecked ?? true,
        EffectIndex = 2
    };

    var translateSettings = new TranslateSettings
    {
        Duration = durationSlider?.Value ?? 0.3,
        TranslateX = translateXSlider?.Value ?? 0.0,
        TranslateY = translateYSlider?.Value ?? 0.0,
        AutoReverse = autoReverseCheckBox?.IsChecked ?? true,
        EffectIndex = 3
    };

    if (animationEffectComboBox != null && animationEffectComboBox.SelectedItem != null)
    {
        var selectedEffectIndex = animationEffectComboBox.SelectedIndex;
        Console.WriteLine($"SelectedEffectIndex in SaveButton_Click: {selectedEffectIndex}");

        var settings = new
        {
            PrimaryColor = primaryColor.ToString(),
            SecondaryColor = secondaryColor.ToString(),
            SelectedEffectIndex = selectedEffectIndex,
            Scale = scaleSettings,
            Rotate = rotateSettings,
            Translate = translateSettings
        };

        string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string directoryPath = Path.Combine(appDataPath, "BiMaDock");

        Directory.CreateDirectory(directoryPath);
        File.WriteAllText(Path.Combine(directoryPath, "StyleSettings.json"), json);

        // Schließen des Fensters
        Close();
    }
    else
    {
        MessageBox.Show("AnimationEffectComboBox oder dessen SelectedItem ist null.");
    }
}




        private void SaveEffectSettings(string filename, object effectSettings)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string directoryPath = Path.Combine(appDataPath, "BiMaDock");
            Directory.CreateDirectory(directoryPath);

            // Serialize and save the settings for a specific animation effect
            string json = JsonConvert.SerializeObject(effectSettings, Formatting.Indented);
            File.WriteAllText(Path.Combine(directoryPath, filename), json);
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
