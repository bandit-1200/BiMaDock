using System.Windows;
using System.Windows.Input;

namespace BiMaDock
{
    /// <summary>
    /// Macht rahmenlose Dialogfenster verschiebbar: Ziehen mit der linken Maustaste an jeder freien Fläche
    /// bewegt das Fenster. Schaltflächen, Eingabefelder usw. behandeln den Klick selbst und sind nicht betroffen.
    /// </summary>
    internal static class DialogWindowBehavior
    {
        public static readonly DependencyProperty IsDraggableProperty = DependencyProperty.RegisterAttached(
            "IsDraggable", typeof(bool), typeof(DialogWindowBehavior), new PropertyMetadata(false, OnIsDraggableChanged));

        public static bool GetIsDraggable(DependencyObject element) => (bool)element.GetValue(IsDraggableProperty);

        public static void SetIsDraggable(DependencyObject element, bool value) => element.SetValue(IsDraggableProperty, value);

        private static void OnIsDraggableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Window window)
            {
                return;
            }

            window.MouseLeftButtonDown -= Window_MouseLeftButtonDown;
            if ((bool)e.NewValue)
            {
                window.MouseLeftButtonDown += Window_MouseLeftButtonDown;
            }
        }

        private static void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled || e.ButtonState != MouseButtonState.Pressed || sender is not Window window)
            {
                return;
            }

            try
            {
                window.DragMove();
            }
            catch (InvalidOperationException)
            {
                // Maustaste wurde bereits losgelassen
            }
        }
    }
}
