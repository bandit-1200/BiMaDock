using System.Windows;

namespace BiMaDock
{
    public partial class TestWindow : Window
    {
        private double testSliderValue = 0;
        private MainWindow mainWindow;

        public TestWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow; // mainWindow zuweisen
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Aktualisiert den Textblock mit dem aktuellen Wert des Sliders
            SliderValueText.Text = ValueSlider.Value.ToString();
            testSliderValue = ValueSlider.Value;


            // Ändert die Margin von CategoryDockBorder im MainWindow
            mainWindow.CategoryDockBorder.Margin = new Thickness(testSliderValue, 0, 0, 0);
            Console.WriteLine($"Slider_ValueChanged: mainWindow.CategoryDockBorder.Margin = { mainWindow.CategoryDockBorder.Margin}");

        }
    }
}
