using System.Windows;
using System.Windows.Media;
using System.Diagnostics;

namespace BiMaDock
{
    public partial class SettingsWindow
    {
        /// <summary>
        /// Wendet gespeicherte Farben, die Einblendverzögerung und die Kategorie-Dock-Animation an, ohne ein Einstellungsfenster zu erzeugen.
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

            mainWindow.SetCategoryFlowAnimation(
                settings.CategoryFlowAnimationEnabled ?? DefaultCategoryFlowAnimationEnabled,
                Math.Clamp(settings.CategoryFlowAnimationMilliseconds ?? DefaultCategoryFlowAnimationMs, MinCategoryFlowAnimationMs, MaxCategoryFlowAnimationMs));

            if (settings.Swing != null)
            {
                ButtonAnimations.SwingSettings.Duration = settings.Swing.Duration ?? ButtonAnimations.SwingSettings.Duration;
                ButtonAnimations.SwingSettings.Angle = settings.Swing.Angle ?? ButtonAnimations.SwingSettings.Angle;
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
    }
}
