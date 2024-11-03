using System.Windows;
using System.Windows.Media;

namespace BiMaDock
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Leere Methode
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
