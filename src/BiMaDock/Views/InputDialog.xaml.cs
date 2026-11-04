using System.Windows;
using System.Windows.Input;

namespace BiMaDock.Views;

/// <summary>
/// Einfacher Eingabedialog im Dock-Design. Verschiebbar an jeder freien Stelle, Esc bricht ab.
/// </summary>
public partial class InputDialog : Window
{
    private InputDialog(string title, string prompt, string initialValue)
    {
        InitializeComponent();
        TitleText.Text = title;
        PromptText.Text = prompt;
        InputBox.Text = initialValue;
        Loaded += (_, _) =>
        {
            InputBox.Focus();
            InputBox.SelectAll();
        };
        UpdateOkButton();
    }

    public string Value => InputBox.Text.Trim();

    /// <summary>Zeigt den Dialog und gibt den eingegebenen Text zurück, oder null bei Abbruch.</summary>
    public static string? Ask(Window? owner, string title, string prompt, string initialValue = "")
    {
        var dialog = new InputDialog(title, prompt, initialValue);
        if (owner is { IsVisible: true })
        {
            dialog.Owner = owner;
        }

        return dialog.ShowDialog() == true ? dialog.Value : null;
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void InputBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => UpdateOkButton();

    private void UpdateOkButton() => OkButton.IsEnabled = Value.Length > 0;

    private void Frame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not System.Windows.Controls.TextBox)
        {
            DragMove();
        }
    }
}
