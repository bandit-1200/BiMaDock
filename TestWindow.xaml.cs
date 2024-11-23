using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading; // Für den DispatcherTimer
using System.Collections;

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
            // mainWindow.CategoryDockBorder.Margin = new Thickness(testSliderValue, 0, 0, 0);

            // Setzt die X-Position von OverlayCanvasHorizontalLine basierend auf dem Slider-Wert
            double overlayPositionX = testSliderValue;
            Canvas.SetLeft(mainWindow.OverlayCanvasHorizontalLine, overlayPositionX);

            // Debug-Ausgabe zur Überprüfung der neuen Margin und Position
            Debug.WriteLine($"Slider_ValueChanged: mainWindow.CategoryDockBorder.Margin = {mainWindow.CategoryDockBorder.Margin}");
            Debug.WriteLine($"Slider_ValueChanged: OverlayCanvasHorizontalLine Position = {overlayPositionX}");
        }


    }
}
