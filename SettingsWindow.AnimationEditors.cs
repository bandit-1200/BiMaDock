using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Diagnostics;

namespace BiMaDock
{
    public partial class SettingsWindow
    {
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
    }
}
