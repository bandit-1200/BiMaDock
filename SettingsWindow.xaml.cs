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
                DialogMessageBox.Show("AnimationEffectComboBox oder dessen SelectedItem ist null.");
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
