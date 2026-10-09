using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace BiMaDock
{
    public partial class SettingsWindow
    {
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
    }
}
