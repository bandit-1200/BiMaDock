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
        private const int MinDockShowDelayMs = 100;
        private const int MaxDockShowDelayMs = 1000;
        private const int DefaultDockShowDelayMs = 300;

        private MainWindow mainWindow;
        private readonly string settingsFilePath;

        // Variablen für die Einstellungen
        private ScaleSettings scaleSettings;
        private RotateSettings rotateSettings;
        private TranslateSettings translateSettings;
        private SwingSettings swingSettings = new();
        private int dockShowDelayMilliseconds = DefaultDockShowDelayMs;

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
            public double TranslateX { get; set; } = 5.0;
            public double TranslateY { get; set; } = -5.0;
            public bool AutoReverse { get; set; } = true;
            public int EffectIndex { get; set; } = 2;
        }

        public class SwingSettings
        {
            public double Duration { get; set; } = 3.5;
            public double Angle { get; set; } = 30;
        }

        private ComboBox? animationEffectComboBox;
        private int animationEffectComboBoxIndex = 0;
        private Slider? scaleFactorSlider;
        private Slider? angleSlider;
        private Slider? translateXSlider;
        private Slider? translateYSlider;
        private Slider? scaleDurationSlider;
        private Slider? rotateDurationSlider;
        private Slider? translateDurationSlider;

        public SettingsWindow(MainWindow window)
        {
            InitializeComponent();

            mainWindow = window;

            DockShowDelaySlider.Minimum = MinDockShowDelayMs;
            DockShowDelaySlider.Maximum = MaxDockShowDelayMs;
            DockShowDelaySlider.Value = dockShowDelayMilliseconds;
            DockShowDelaySlider.ValueChanged += DockShowDelaySlider_ValueChanged;
            UpdateDockShowDelayText();

            // Initialisieren der Einstellungsvariablen
            scaleSettings = new ScaleSettings();
            rotateSettings = new RotateSettings();
            translateSettings = new TranslateSettings();

            settingsFilePath = AppPaths.GetSettingsFilePath("StyleSettings.json");
            CreateAnimationEffectDropdown();
            InitializeColorPickers();
            AutoStartCheckBox.IsChecked = StartupManager.IsInStartup();
            LoadSettings();

        }

        private void UpdateDockShowDelayText()
        {
            if (DockShowDelaySlider == null || DockShowDelayValueText == null)
            {
                return;
            }

            var value = (int)Math.Round(DockShowDelaySlider.Value);
            dockShowDelayMilliseconds = value;
            DockShowDelayValueText.Text = $"Aktuelle Einblendzeit: {dockShowDelayMilliseconds} ms";
        }

        private void DockShowDelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DockShowDelaySlider == null || DockShowDelayValueText == null)
            {
                return;
            }

            var clampedValue = Math.Clamp(DockShowDelaySlider.Value, MinDockShowDelayMs, MaxDockShowDelayMs);
            if (Math.Abs(DockShowDelaySlider.Value - clampedValue) > 0.001)
            {
                DockShowDelaySlider.Value = clampedValue;
                return;
            }

            UpdateDockShowDelayText();
            if (mainWindow != null)
            {
                mainWindow.SetDockShowDelayMilliseconds(dockShowDelayMilliseconds);
            }
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
                    CreateSwingAnimationSettings();
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
                rotateSettings.Duration = rotateDurationSlider.Value;
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
                TickFrequency = 2.0,
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
                rotateSettings.Angle = angleSlider.Value;
            };

            // Initialisiere und speichere die Referenz auf die globale CheckBox für AutoReverse
            var rotateAutoReverseCheckBox = new CheckBox
            {
                Content = "Rotate AutoReverse", // Benennen der CheckBox für späteren Zugriff
                Foreground = Brushes.White,
                IsChecked = rotateSettings.AutoReverse, // Wert aus den Einstellungen laden
                Margin = new Thickness(0, 0, 0, 20)
            };

            // Gemeinsamen Event-Handler für Checked und Unchecked hinzufügen
            rotateAutoReverseCheckBox.Checked += (s, e) =>
            {
                rotateSettings.AutoReverse = true;
                Debug.WriteLine("rotate AutoReverse aktiviert");
            };

            rotateAutoReverseCheckBox.Unchecked += (s, e) =>
            {
                rotateSettings.AutoReverse = false;
                Debug.WriteLine("rotate AutoReverse deaktiviert");
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
                Maximum = 2.0,
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
                translateSettings.Duration = translateDurationSlider.Value;
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
                TickFrequency = 1.0,
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
                translateSettings.TranslateX = translateXSlider.Value;
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
                TickFrequency = 1.0,
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
                translateSettings.TranslateY = translateYSlider.Value;

            };

            // Initialisiere und speichere die Referenz auf die globale CheckBox für AutoReverse
            var translateAutoReverseCheckBox = new CheckBox
            {
                Content = "translate AutoReverse", // Benennen der CheckBox für späteren Zugriff
                Foreground = Brushes.White,
                IsChecked = translateSettings.AutoReverse, // Wert aus den Einstellungen laden
                Margin = new Thickness(0, 0, 0, 20)
            };

            // Gemeinsamen Event-Handler für Checked und Unchecked hinzufügen
            translateAutoReverseCheckBox.Checked += (s, e) =>
            {
                translateSettings.AutoReverse = true;
                Debug.WriteLine("translate AutoReverse aktiviert");
            };

            translateAutoReverseCheckBox.Unchecked += (s, e) =>
            {
                translateSettings.AutoReverse = false;
                Debug.WriteLine("translate AutoReverse deaktiviert");
            };
            AnimationSettingsPanel.Children.Add(translateAutoReverseCheckBox);
        }

        private void CreateSwingAnimationSettings()
        {
            var durationLabel = new TextBlock
            {
                Text = "Swing-Dauer (Sekunden):",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            AnimationSettingsPanel.Children.Add(durationLabel);

            var durationSlider = new Slider
            {
                Minimum = 0.5,
                Maximum = 7,
                Value = swingSettings.Duration,
                TickFrequency = 0.1,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(durationSlider);
            var durationValue = new TextBlock
            {
                Text = $"Aktuelle Dauer: {durationSlider.Value:0.0} s",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(durationValue);
            durationSlider.ValueChanged += (_, _) =>
            {
                swingSettings.Duration = durationSlider.Value;
                ButtonAnimations.SwingSettings.Duration = durationSlider.Value;
                durationValue.Text = $"Aktuelle Dauer: {durationSlider.Value:0.0} s";
            };

            var angleLabel = new TextBlock
            {
                Text = "Swing-Winkel:",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            AnimationSettingsPanel.Children.Add(angleLabel);

            var angleSlider = new Slider
            {
                Minimum = 5,
                Maximum = 60,
                Value = swingSettings.Angle,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(angleSlider);
            var angleValue = new TextBlock
            {
                Text = $"Aktueller Winkel: {angleSlider.Value:0}°",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };
            AnimationSettingsPanel.Children.Add(angleValue);
            angleSlider.ValueChanged += (_, _) =>
            {
                swingSettings.Angle = angleSlider.Value;
                ButtonAnimations.SwingSettings.Angle = angleSlider.Value;
                angleValue.Text = $"Aktueller Winkel: {angleSlider.Value:0}°";
            };
        }



        /// <summary>
        /// Wendet gespeicherte Farben und die Einblendverzögerung an, ohne ein Einstellungsfenster zu erzeugen.
        /// </summary>
        public static void ApplyStyleSettings(MainWindow mainWindow)
        {
            var settings = StyleSettingsFile.Load();
            if (settings == null)
            {
                return;
            }

            var resources = Application.Current.Resources;
            if (settings.PrimaryColor != null && ColorConverter.ConvertFromString(settings.PrimaryColor) is Color primaryColor)
            {
                var newPrimaryColor = new SolidColorBrush(primaryColor);
                resources["PrimaryColor"] = newPrimaryColor;
                mainWindow.DockPanel.Background = newPrimaryColor;
                mainWindow.CategoryDockBorder.Background = newPrimaryColor;
                mainWindow.OverlayCanvasHorizontalLine.Stroke = newPrimaryColor;
            }

            if (settings.SecondaryColor != null && ColorConverter.ConvertFromString(settings.SecondaryColor) is Color secondaryColor)
            {
                resources["SecondaryColor"] = new SolidColorBrush(secondaryColor);
            }

            if (settings.AccentColor != null && ColorConverter.ConvertFromString(settings.AccentColor) is Color accentColor)
            {
                resources["AccentColor"] = new SolidColorBrush(accentColor);
            }

            if (settings.FeedbackColor != null && ColorConverter.ConvertFromString(settings.FeedbackColor) is Color feedbackColor)
            {
                resources["FeedbackColor"] = new SolidColorBrush(feedbackColor);
            }

            if (settings.DockShowDelayMilliseconds is int dockShowDelay)
            {
                mainWindow.SetDockShowDelayMilliseconds(Math.Clamp(dockShowDelay, MinDockShowDelayMs, MaxDockShowDelayMs));
            }

            if (settings.Swing != null)
            {
                ButtonAnimations.SwingSettings.Duration = settings.Swing.Duration ?? ButtonAnimations.SwingSettings.Duration;
                ButtonAnimations.SwingSettings.Angle = settings.Swing.Angle ?? ButtonAnimations.SwingSettings.Angle;
            }
        }

        public void LoadSettings()
        {
            {
                var settings = StyleSettingsFile.Load(settingsFilePath);

                if (settings != null)
                {
                    // Farben laden
                    if (settings.PrimaryColor != null && ColorConverter.ConvertFromString(settings.PrimaryColor) is Color primaryColor)
                    {
                        // Farbwähler und Vorschau aktualisieren
                        PrimaryColorPicker.SelectedColor = primaryColor;
                        PrimaryColorPreview.Background = new SolidColorBrush(primaryColor);

                        // Ressourcen aktualisieren
                        var newPrimaryColor = new SolidColorBrush(primaryColor);
                        Application.Current.Resources["PrimaryColor"] = newPrimaryColor;

                        mainWindow.DockPanel.Background = newPrimaryColor;
                        mainWindow.CategoryDockBorder.Background = newPrimaryColor;
                        mainWindow.OverlayCanvasHorizontalLine.Stroke = newPrimaryColor;



                    }

                    if (settings.SecondaryColor != null
                        && ColorConverter.ConvertFromString(settings.SecondaryColor) is Color secondaryColor)
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



                    if (settings.AccentColor != null
                        && ColorConverter.ConvertFromString(settings.AccentColor) is Color accentColor)
                    {
                        // Farbwähler und Vorschau aktualisieren
                        AccentColorPicker.SelectedColor = accentColor;
                        // AccentColorPreview.Background = new SolidColorBrush(accentColorColor);

                        // Ressourcen aktualisieren
                        var newAccentColor = new SolidColorBrush(accentColor);
                        Application.Current.Resources["AccentColor"] = newAccentColor;

                        // Debugging
                        Debug.WriteLine($"LoadSettings: AccentColor {settings.AccentColor}");
                    }



                    if (settings.FeedbackColor != null
                        && ColorConverter.ConvertFromString(settings.FeedbackColor) is Color feedbackColor)
                    {
                        // Farbwähler und Vorschau aktualisieren
                        FeedbackColorPicker.SelectedColor = feedbackColor;


                        // Ressourcen aktualisieren
                        var newFeedbackColor = new SolidColorBrush(feedbackColor);
                        Application.Current.Resources["FeedbackColor"] = newFeedbackColor;

                        // Debugging
                        Debug.WriteLine($"LoadSettings: FeedbackColor {settings.FeedbackColor}");
                    }



                    if (settings.DockShowDelayMilliseconds is int savedDockShowDelay)
                    {
                        dockShowDelayMilliseconds = Math.Clamp(savedDockShowDelay, MinDockShowDelayMs, MaxDockShowDelayMs);
                        if (DockShowDelaySlider != null)
                        {
                            DockShowDelaySlider.Value = dockShowDelayMilliseconds;
                        }
                        mainWindow.SetDockShowDelayMilliseconds(dockShowDelayMilliseconds);
                        UpdateDockShowDelayText();
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

                    if (settings.Swing != null)
                    {
                        swingSettings.Duration = settings.Swing.Duration ?? swingSettings.Duration;
                        swingSettings.Angle = settings.Swing.Angle ?? swingSettings.Angle;
                        ButtonAnimations.SwingSettings.Duration = swingSettings.Duration;
                        ButtonAnimations.SwingSettings.Angle = swingSettings.Angle;
                    }

                    // Effekt-Index laden
                    if (settings.SelectedEffectIndex is int selectedEffectIndex)
                    {
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

        private void InitializeColorPickers()
        {
            PrimaryColorPicker.SelectedColor = GetResourceColor("PrimaryColor");
            SecondaryColorPicker.SelectedColor = GetResourceColor("SecondaryColor");
            AccentColorPicker.SelectedColor = GetResourceColor("AccentColor");
            FeedbackColorPicker.SelectedColor = GetResourceColor("FeedbackColor");
        }

        private static Color GetResourceColor(string resourceKey)
        {
            if (Application.Current.Resources[resourceKey] is not SolidColorBrush brush)
            {
                throw new InvalidOperationException($"Color resource '{resourceKey}' is missing or is not a SolidColorBrush.");
            }

            return brush.Color;
        }


        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var primaryColor = PrimaryColorPicker.SelectedColor ?? GetResourceColor("PrimaryColor");
            var secondaryColor = SecondaryColorPicker.SelectedColor ?? GetResourceColor("SecondaryColor");
            var accentColor = AccentColorPicker.SelectedColor ?? GetResourceColor("AccentColor");
            var feedbackColor = FeedbackColorPicker.SelectedColor ?? GetResourceColor("FeedbackColor");
            primaryColor.A = Math.Max(primaryColor.A, (byte)1);
            secondaryColor.A = Math.Max(secondaryColor.A, (byte)1);
            accentColor.A = Math.Max(accentColor.A, (byte)1);
            feedbackColor.A = Math.Max(feedbackColor.A, (byte)1);

            if (animationEffectComboBox != null && animationEffectComboBox.SelectedItem != null)
            {
                var selectedEffectIndex = animationEffectComboBox.SelectedIndex;
                Debug.WriteLine($"SelectedEffectIndex in SaveButton_Click: {selectedEffectIndex}");

                var dockShowDelay = (int)Math.Round(DockShowDelaySlider?.Value ?? dockShowDelayMilliseconds);
                dockShowDelay = Math.Clamp(dockShowDelay, MinDockShowDelayMs, MaxDockShowDelayMs);
                dockShowDelayMilliseconds = dockShowDelay;

                // Setze den festen Effektindex für jede Animation
                scaleSettings.EffectIndex = 1;
                rotateSettings.EffectIndex = 2;
                translateSettings.EffectIndex = 3;

                var settings = new
                {
                    PrimaryColor = primaryColor.ToString(),
                    SecondaryColor = secondaryColor.ToString(),
                    AccentColor = accentColor.ToString(),
                    FeedbackColor = feedbackColor.ToString(),
                    DockShowDelayMilliseconds = dockShowDelay,
                    SelectedEffectIndex = selectedEffectIndex,
                    Scale = scaleSettings,
                    Rotate = rotateSettings,
                    Translate = translateSettings,
                    Swing = swingSettings
                };

                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                string directoryPath = AppPaths.EnsureAppDataDirectory();

                await File.WriteAllTextAsync(Path.Combine(directoryPath, "StyleSettings.json"), json);

                // Gespeicherte Werte anwenden, dann das Fenster schließen
                ButtonAnimations.LoadSettings();
                ApplyStyleSettings(mainWindow);
                Close();
            }
            else
            {
                MessageBox.Show("AnimationEffectComboBox oder dessen SelectedItem ist null.");
            }
        }


        private void PrimaryColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
            {
                var selectedColor = e.NewValue.Value;

                // Überprüfen, ob der Alpha-Wert mindestens 1 ist
                if (selectedColor.A < 1)
                {
                    selectedColor.A = 1; // Mindestwert der Alpha-Komponente auf 1 setzen
                }

                PrimaryColorPicker.Background = new SolidColorBrush(selectedColor);
                PrimaryColorPreview.Background = new SolidColorBrush(selectedColor);
            }
        }
        private void AccentColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
            {
                var selectedColor = e.NewValue.Value;
                Debug.WriteLine($"AccentColorPicker_SelectedColorChanged: {selectedColor}");
                // Überprüfen, ob der Alpha-Wert mindestens 1 ist
                if (selectedColor.A < 1)
                {
                    selectedColor.A = 1; // Mindestwert der Alpha-Komponente auf 1 setzen
                }

                AccentColorPicker.Background = new SolidColorBrush(selectedColor);
                // AccentColorPicker.Background = new SolidColorBrush(selectedColor);

            }
        }

        private void FeedbackColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
            {
                var selectedColor = e.NewValue.Value;
                Debug.WriteLine($"FeedbackColorPicker_SelectedColorChanged: {selectedColor}");
                // Überprüfen, ob der Alpha-Wert mindestens 1 ist
                if (selectedColor.A < 1)
                {
                    selectedColor.A = 1; // Mindestwert der Alpha-Komponente auf 1 setzen
                }

                FeedbackColorPicker.Background = new SolidColorBrush(selectedColor);
                // FeedbackColorPicker.Background = new SolidColorBrush(selectedColor);

            }
        }







        private void SecondaryColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
            {
                var selectedColor = e.NewValue.Value;

                // Überprüfen, ob der Alpha-Wert mindestens 1 ist
                if (selectedColor.A < 1)
                {
                    selectedColor.A = 1; // Mindestwert der Alpha-Komponente auf 1 setzen
                }

                SecondaryColorPicker.Background = new SolidColorBrush(selectedColor);
                SecondaryColorPreview.Background = new SolidColorBrush(selectedColor);
            }
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
