using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;
using System.IO;
using System.Windows.Controls;
using System.Diagnostics;



namespace BiMaDock
{
    public partial class SettingsWindow : Window
    {
        private MainWindow mainWindow;
        private readonly string settingsFilePath;

        // Variablen für die Einstellungen
        private ScaleSettings scaleSettings;
        private RotateSettings rotateSettings;
        private TranslateSettings translateSettings;

        public class ScaleSettings
        {
            public double Duration { get; set; } = 0.3;
            public double ScaleFactor { get; set; } = 1.2;
            public bool AutoReverse { get; set; } = true;
            public int EffectIndex { get; set; } = 0;
        }



        public class RotateSettings
        {
            public double Duration { get; set; } = 0.3;
            public double Angle { get; set; } = 180.0;
            public bool AutoReverse { get; set; } = true;
            public int EffectIndex { get; set; } = 1;
        }


        public class TranslateSettings
        {
            public double Duration { get; set; } = 0.3;
            public double TranslateX { get; set; } = 0.0;
            public double TranslateY { get; set; } = 0.0;
            public bool AutoReverse { get; set; } = true;
            public int EffectIndex { get; set; } = 2;
        }

        private ComboBox? animationEffectComboBox;
        private int animationEffectComboBoxIndex = 0;
        private Slider? scaleFactorSlider;
        private Slider? angleSlider;
        private Slider? translateXSlider;
        private Slider? translateYSlider;
        private CheckBox? autoReverseCheckBox;
        private Slider? scaleDurationSlider;
        private Slider? rotateDurationSlider;
        private Slider? translateDurationSlider;




        public SettingsWindow(MainWindow window)
        {
            InitializeComponent();

            mainWindow = window;

            // Initialisieren der Einstellungsvariablen
            scaleSettings = new ScaleSettings();
            rotateSettings = new RotateSettings();
            translateSettings = new TranslateSettings();

            // Initialisieren der Steuerelemente
            // PrimaryColorPicker = new ColorPicker();
            // SecondaryColorPicker = new ColorPicker();
            scaleDurationSlider = new Slider();
            scaleFactorSlider = new Slider();
            autoReverseCheckBox = new CheckBox();
            rotateDurationSlider = new Slider();
            angleSlider = new Slider();
            translateDurationSlider = new Slider();
            translateXSlider = new Slider();
            translateYSlider = new Slider();
            animationEffectComboBox = new ComboBox();


            // animationEffectComboBox = new ComboBox();
            settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BiMaDock", "StyleSettings.json");
            CreateAnimationEffectDropdown();
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
            animationEffectComboBox.Items.Add("Swing");
            animationEffectComboBox.SelectedIndex = animationEffectComboBoxIndex;  // Setze den initialen Index auf "Kein Effekt"
            animationEffectComboBox.SelectionChanged += AnimationEffectComboBox_SelectionChanged;
            AnimationSettingsPanel.Children.Add(animationEffectComboBox);

            // Initiale Einstellungen erstellen (z.B. Scale)
            // CreateScaleAnimationSettings();
        }



        private void AnimationEffectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                int selectedIndex = comboBox.SelectedIndex;

                // Überprüfe, ob ein gültiger Index ausgewählt wurde
                if (selectedIndex >= 0)
                {
                    // Verwende die neue Methode, um die Animationseinstellungen zu aktualisieren
                    UpdateAnimationSettings(selectedIndex);
                }
            }
        }




        private void UpdateAnimationSettings(int selectedIndex)
        {
            // Entferne nur die dynamisch erstellten Kinder, nicht das Dropdown-Menü
            while (AnimationSettingsPanel.Children.Count > 1) // Da das Dropdown-Menü das zweite Element ist
            {
                AnimationSettingsPanel.Children.RemoveAt(1);
            }

            // Logik zur Erstellung der Animationseinstellungen basierend auf dem Index
            switch (selectedIndex)
            {
                case 1: // Index für Scale
                    CreateScaleAnimationSettings();
                    break;
                case 2: // Index für Rotate
                    CreateRotateAnimationSettings();
                    break;
                case 3: // Index für Translate
                    CreateTranslateAnimationSettings();
                    break;
                case 4: // Index für Swing
                        // CreateSwingAnimationSettings();
                    break;
                default:
                    Debug.WriteLine($"Kein gültiger Effekt ausgewählt: {selectedIndex}");
                    break;
            }

            // Ausgabe des aktuellen SelectedIndex auf der Konsole
            Debug.WriteLine($"SelectedIndex: {selectedIndex}");
        }






        private void CreateScaleAnimationSettings()
        {
            // Slider für die Animationsdauer
            TextBlock scaleDurationTextBlock = new TextBlock
            {
                Text = "Dauer (in Sekunden scale):",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            AnimationSettingsPanel.Children.Add(scaleDurationTextBlock);

            // Initialisiere und speichere die Referenz auf den globalen Slider für die Dauer
            scaleDurationSlider = new Slider
            {
                Minimum = 0.1,
                Maximum = 3.0,
                Value = scaleSettings.Duration, // Wert aus den Einstellungen laden
                TickFrequency = 0.1,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(scaleDurationSlider);

            // TextBlock zur Anzeige des aktuellen Werts des Sliders
            TextBlock durationValueTextBlock = new TextBlock
            {
                Text = $"Aktuelle Dauer: {scaleDurationSlider.Value} s",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(durationValueTextBlock);

            // Event-Handler zur Aktualisierung des TextBlocks bei Änderung des Slider-Werts
            scaleDurationSlider.ValueChanged += (s, e) =>
            {
                durationValueTextBlock.Text = $"Aktuelle Dauer: {scaleDurationSlider.Value} s";
                scaleSettings.Duration = scaleDurationSlider.Value;
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
                Value = scaleSettings.ScaleFactor, // Wert aus den Einstellungen laden
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
                scaleSettings.ScaleFactor = scaleFactorSlider.Value;
            };

            // Initialisiere und speichere die Referenz auf die globale CheckBox für AutoReverse
            var scaleAutoReverseCheckBox = new CheckBox
            {
                Content = "Scale AutoReverse", // Benennen der CheckBox für späteren Zugriff
                Foreground = Brushes.White,
                IsChecked = scaleSettings.AutoReverse, // Wert aus den Einstellungen laden
                Margin = new Thickness(0, 0, 0, 20)
            };

            // Gemeinsamen Event-Handler für Checked und Unchecked hinzufügen
            scaleAutoReverseCheckBox.Checked += (s, e) =>
            {
                scaleSettings.AutoReverse = true;
                Debug.WriteLine("Scale AutoReverse aktiviert");
            };

            scaleAutoReverseCheckBox.Unchecked += (s, e) =>
            {
                scaleSettings.AutoReverse = false;
                Debug.WriteLine("Scale AutoReverse deaktiviert");
            };

            AnimationSettingsPanel.Children.Add(scaleAutoReverseCheckBox);

        }

        private void CreateRotateAnimationSettings()
        {
            // Slider für die Animationsdauer
            TextBlock rotateDurationTextBlock = new TextBlock
            {
                Text = "Dauer (in Sekunden CreateRotateAnimationSettings):",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            AnimationSettingsPanel.Children.Add(rotateDurationTextBlock);

            // Initialisiere und speichere die Referenz auf den globalen Slider für die Dauer
            rotateDurationSlider = new Slider
            {
                Minimum = 0.1,
                Maximum = 3.0,
                Value = rotateSettings.Duration, // Wert aus den Einstellungen laden
                TickFrequency = 0.1,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(rotateDurationSlider);

            // TextBlock zur Anzeige des aktuellen Werts des Sliders
            TextBlock durationValueTextBlock = new TextBlock
            {
                Text = $"Aktuelle Dauer: {rotateDurationSlider.Value} s",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(durationValueTextBlock);

            // Event-Handler zur Aktualisierung des TextBlocks bei Änderung des Slider-Werts
            rotateDurationSlider.ValueChanged += (s, e) =>
            {
                durationValueTextBlock.Text = $"Aktuelle Dauer: {rotateDurationSlider.Value} s";
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
                Value = rotateSettings.Angle, // Wert aus den Einstellungen laden
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
            var rotateAutoReverseCheckBox = new CheckBox
            {
                Content = "Rotate AutoReverse", // Benennen der CheckBox für späteren Zugriff
                Foreground = Brushes.White,
                IsChecked = rotateSettings.AutoReverse, // Wert aus den Einstellungen laden
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(rotateAutoReverseCheckBox);
        }


        private void CreateTranslateAnimationSettings()
        {
            // Slider für die Animationsdauer
            TextBlock translateDurationTextBlock = new TextBlock
            {
                Text = "Dauer (in Sekunden CreateTranslateAnimationSettings):",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            AnimationSettingsPanel.Children.Add(translateDurationTextBlock);

            // Initialisiere und speichere die Referenz auf den globalen Slider für die Dauer
            translateDurationSlider = new Slider
            {
                Minimum = 0.1,
                Maximum = 3.0,
                Value = translateSettings.Duration, // Wert aus den Einstellungen laden
                TickFrequency = 0.1,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(translateDurationSlider);

            // TextBlock zur Anzeige des aktuellen Werts des Sliders
            TextBlock durationValueTextBlock = new TextBlock
            {
                Text = $"Aktuelle Dauer: {translateDurationSlider.Value} s",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(durationValueTextBlock);

            // Event-Handler zur Aktualisierung des TextBlocks bei Änderung des Slider-Werts
            translateDurationSlider.ValueChanged += (s, e) =>
            {
                durationValueTextBlock.Text = $"Aktuelle Dauer: {translateDurationSlider.Value} s";
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
                Value = translateSettings.TranslateX, // Wert aus den Einstellungen laden
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
                Value = translateSettings.TranslateY, // Wert aus den Einstellungen laden
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
            var translateAutoReverseCheckBox = new CheckBox
            {
                Content = "Translate AutoReverse", // Benennen der CheckBox für späteren Zugriff
                Foreground = Brushes.White,
                IsChecked = translateSettings.AutoReverse, // Wert aus den Einstellungen laden
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(translateAutoReverseCheckBox);
        }



        private void ShowVersionInConsole()
        {
            var informationalVersionAttribute = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            string informationalVersion = informationalVersionAttribute?.InformationalVersion ?? "Unbekannte Version";
            string clearVersion = informationalVersion.Split('+')[0];

            Debug.WriteLine($"Detaillierte Version: {informationalVersion}");
            Debug.WriteLine($"Klare Version: {clearVersion}");
        }
        public void LoadSettings()
        {
            if (File.Exists(settingsFilePath))
            {
                string json = File.ReadAllText(settingsFilePath);
                var settings = JsonConvert.DeserializeObject<dynamic>(json);
                var resources = Application.Current.Resources;

                if (settings != null)
                {
                    // Farben laden
                    if (settings.PrimaryColor != null && ColorConverter.ConvertFromString((string)settings.PrimaryColor) is Color primaryColor)
                    {
                        // Farbwähler und Vorschau aktualisieren
                        PrimaryColorPicker.SelectedColor = primaryColor;
                        PrimaryColorPreview.Background = new SolidColorBrush(primaryColor);

                        // Ressourcen aktualisieren
                        var newPrimaryColor = new SolidColorBrush(primaryColor);
                        Application.Current.Resources["PrimaryColor"] = newPrimaryColor;
                        // resources["PrimaryColor"] = new SolidColorBrush(primaryColor);

                        mainWindow.DockPanel.Background = settings.PrimaryColor;
                        mainWindow.CategoryDockBorder.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
                        mainWindow.OverlayCanvasHorizontalLine.Stroke = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];



                    }

                    if (settings.SecondaryColor != null
                        && ColorConverter.ConvertFromString((string)settings.SecondaryColor) is Color secondaryColor)
                    {
                        // Farbwähler und Vorschau aktualisieren
                        SecondaryColorPicker.SelectedColor = secondaryColor;
                        SecondaryColorPreview.Background = new SolidColorBrush(secondaryColor);

                        // Ressourcen aktualisieren
                        var newSecondaryColor = new SolidColorBrush(secondaryColor);
                        Application.Current.Resources["SecondaryColor"] = newSecondaryColor;

                        // Debugging
                        Debug.WriteLine($"LoadSettings: SecondaryColor {settings.SecondaryColor}");
                    }


                    // Animationseinstellungen laden
                    if (settings.Scale != null)
                    {
                        scaleSettings.Duration = settings.Scale.Duration ?? scaleSettings.Duration;
                        scaleSettings.ScaleFactor = settings.Scale.ScaleFactor ?? scaleSettings.ScaleFactor;
                        scaleSettings.AutoReverse = settings.Scale.AutoReverse ?? scaleSettings.AutoReverse;
                        scaleSettings.EffectIndex = 1; // Fester Wert für Scale
                    }

                    if (settings.Rotate != null)
                    {
                        rotateSettings.Duration = settings.Rotate.Duration ?? rotateSettings.Duration;
                        rotateSettings.Angle = settings.Rotate.Angle ?? rotateSettings.Angle;
                        rotateSettings.AutoReverse = settings.Rotate.AutoReverse ?? rotateSettings.AutoReverse;
                        rotateSettings.EffectIndex = 2; // Fester Wert für Rotate
                    }

                    if (settings.Translate != null)
                    {
                        translateSettings.Duration = settings.Translate.Duration ?? translateSettings.Duration;
                        translateSettings.TranslateX = settings.Translate.TranslateX ?? translateSettings.TranslateX;
                        translateSettings.TranslateY = settings.Translate.TranslateY ?? translateSettings.TranslateY;
                        translateSettings.AutoReverse = settings.Translate.AutoReverse ?? translateSettings.AutoReverse;
                        translateSettings.EffectIndex = 3; // Fester Wert für Translate
                    }

                    // Effekt-Index laden
                    if (settings.SelectedEffectIndex != null)
                    {
                        var selectedEffectIndex = (int)settings.SelectedEffectIndex;
                        if (animationEffectComboBox != null)
                        {
                            animationEffectComboBoxIndex = selectedEffectIndex;
                            animationEffectComboBox.SelectedIndex = animationEffectComboBoxIndex;
                            UpdateAnimationSettings(animationEffectComboBoxIndex);
                        }
                    }
                }
            }
        }


        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var primaryColor = PrimaryColorPicker.SelectedColor ?? Colors.Transparent;
            var secondaryColor = SecondaryColorPicker.SelectedColor ?? Colors.Transparent;

            // Aktualisiere die Variablen mit den Werten der Steuerelemente
            // if (scaleDurationSlider != null) scaleSettings.Duration = scaleDurationSlider.Value;
            // if (scaleFactorSlider != null) scaleSettings.ScaleFactor = scaleFactorSlider.Value;

            // // Hier speziell die CheckBox für Scale verwenden
            // var scaleAutoReverseCheckBox = AnimationSettingsPanel.Children.OfType<CheckBox>().FirstOrDefault(chk => chk.Content.ToString() == "Scale AutoReverse");
            // if (scaleAutoReverseCheckBox != null) scaleSettings.AutoReverse = scaleAutoReverseCheckBox.IsChecked ?? true;

            // if (rotateDurationSlider != null) rotateSettings.Duration = rotateDurationSlider.Value;
            // if (angleSlider != null) rotateSettings.Angle = angleSlider.Value;

            // // Hier speziell die CheckBox für Rotate verwenden
            // var rotateAutoReverseCheckBox = AnimationSettingsPanel.Children.OfType<CheckBox>().FirstOrDefault(chk => chk.Content.ToString() == "Rotate AutoReverse");
            // if (rotateAutoReverseCheckBox != null) rotateSettings.AutoReverse = rotateAutoReverseCheckBox.IsChecked ?? true;

            // if (translateDurationSlider != null) translateSettings.Duration = translateDurationSlider.Value;
            // if (translateXSlider != null) translateSettings.TranslateX = translateXSlider.Value;
            // if (translateYSlider != null) translateSettings.TranslateY = translateYSlider.Value;

            // // Hier speziell die CheckBox für Translate verwenden
            // var translateAutoReverseCheckBox = AnimationSettingsPanel.Children.OfType<CheckBox>().FirstOrDefault(chk => chk.Content.ToString() == "Translate AutoReverse");
            // if (translateAutoReverseCheckBox != null) translateSettings.AutoReverse = translateAutoReverseCheckBox.IsChecked ?? true;

            if (animationEffectComboBox != null && animationEffectComboBox.SelectedItem != null)
            {
                var selectedEffectIndex = animationEffectComboBox.SelectedIndex;
                Debug.WriteLine($"SelectedEffectIndex in SaveButton_Click: {selectedEffectIndex}");

                // Setze den festen Effektindex für jede Animation
                scaleSettings.EffectIndex = 1;
                rotateSettings.EffectIndex = 2;
                translateSettings.EffectIndex = 3;

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
                await File.WriteAllTextAsync(Path.Combine(directoryPath, "StyleSettings.json"), json);

                // Schließen des Fensters
                ButtonAnimations.LoadSettings();
                Close();
            }
            else
            {
                MessageBox.Show("AnimationEffectComboBox oder dessen SelectedItem ist null.");
            }

            LoadSettings();
        }


        // private void SaveEffectSettings(string filename, object effectSettings)
        // {
        //     string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        //     string directoryPath = Path.Combine(appDataPath, "BiMaDock");
        //     Directory.CreateDirectory(directoryPath);

        //     // Serialize and save the settings for a specific animation effect
        //     string json = JsonConvert.SerializeObject(effectSettings, Formatting.Indented);
        //     File.WriteAllText(Path.Combine(directoryPath, filename), json);
        // }



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
