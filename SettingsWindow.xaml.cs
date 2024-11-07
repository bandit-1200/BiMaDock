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

        public SettingsWindow()
        {
            InitializeComponent();
            CreateAnimationEffectDropdown();
            settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BiMaDock", "StyleSettings.json");

            ShowVersionInConsole();
            AutoStartCheckBox.IsChecked = StartupManager.IsInStartup();
            LoadSettings();
        }


        
private void CreateAnimationEffectDropdown()
{
    // Container für die dynamisch erstellten Steuerelemente
    StackPanel animationSettingsPanel = new StackPanel();

    // Titel für das Animations-Tab
    TextBlock titleTextBlock = new TextBlock
    {
        Text = "Animationseinstellungen",
        FontSize = 20,
        FontWeight = FontWeights.Bold,
        Foreground = Brushes.White,
        HorizontalAlignment = HorizontalAlignment.Center,
        Margin = new Thickness(0, 0, 0, 20)
    };
    animationSettingsPanel.Children.Add(titleTextBlock);

    // ComboBox für die Auswahl des Animationseffekts
    ComboBox animationEffectComboBox = new ComboBox
    {
        Name = "AnimationEffectComboBox", // Name zur Identifikation im Event-Handler
        Margin = new Thickness(0, 0, 0, 20)
    };
    animationEffectComboBox.Items.Add("Scale");
    animationEffectComboBox.Items.Add("Rotate");
    animationEffectComboBox.Items.Add("Translate");
    animationEffectComboBox.SelectedIndex = 0; // Standardmäßig "Scale" ausgewählt
    animationEffectComboBox.SelectionChanged += AnimationEffectComboBox_SelectionChanged; // Event-Handler hinzufügen
    animationSettingsPanel.Children.Add(animationEffectComboBox);

    // Füge das dynamisch erstellte Panel dem Platzhalter hinzu
    AnimationSettingsPanel.Children.Add(animationSettingsPanel);
}

private void AnimationEffectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (sender is ComboBox comboBox)
    {
        // Überprüfe, ob SelectedItem nicht null ist
        string? selectedEffect = comboBox.SelectedItem?.ToString();
        
        if (!string.IsNullOrEmpty(selectedEffect))
        {
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

    Slider durationSlider = new Slider
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

    Slider scaleFactorSlider = new Slider
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

    // CheckBox für AutoReverse
    CheckBox autoReverseCheckBox = new CheckBox
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

    Slider durationSlider = new Slider
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

    Slider angleSlider = new Slider
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

    // CheckBox für AutoReverse
    CheckBox autoReverseCheckBox = new CheckBox
    {
        Content = "Automatisches Rückwärtslaufen (AutoReverse)",
        Foreground = Brushes.White,
        IsChecked = true,
        Margin = new Thickness(0, 0, 0, 20)
    };
    AnimationSettingsPanel.Children.Add(autoReverseCheckBox);
}

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

    Slider durationSlider = new Slider
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

    Slider translateXSlider = new Slider
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

    Slider translateYSlider = new Slider
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

    // CheckBox für AutoReverse
    CheckBox autoReverseCheckBox = new CheckBox
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

            var settings = new
            {
                PrimaryColor = primaryColor.ToString(),
                SecondaryColor = secondaryColor.ToString(),
                // FadeDuration = FadeDurationSlider?.Value ?? 1.0
            };

            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string directoryPath = Path.Combine(appDataPath, "BiMaDock");

            Directory.CreateDirectory(directoryPath);
            File.WriteAllText(Path.Combine(directoryPath, "StyleSettings.json"), json);
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
