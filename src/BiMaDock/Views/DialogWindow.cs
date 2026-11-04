using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

namespace BiMaDock.Views;

/// <summary>
/// Gemeinsame Basis der Dialoge: rahmenlos, im Dock-Design, an jeder freien Stelle verschiebbar, Esc schließt.
/// </summary>
public class DialogWindow : Window
{
    public DialogWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = null;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        UseLayoutRounding = true;
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
        };
        MouseLeftButtonDown += (_, e) =>
        {
            if (e.OriginalSource is not (TextBoxBase or ButtonBase or Thumb or Slider))
            {
                DragMove();
            }
        };
    }

    /// <summary>Zeigt den Dialog über dem Dock, falls dieses sichtbar ist.</summary>
    public bool? ShowFor(Window? owner)
    {
        if (owner is { IsVisible: true })
        {
            Owner = owner;
        }

        return ShowDialog();
    }
}
