using System.Windows;
using BiMaDock.Core.Settings;

namespace BiMaDock.Views;

public partial class SettingsDialog : DialogWindow
{
    private SettingsDialog(AppSettings settings, bool autostart)
    {
        InitializeComponent();
        Apply(settings);
        Autostart.IsChecked = autostart;
    }

    private AppSettings Result => new()
    {
        ShowDelayMs = (int)ShowDelay.Value,
        HideDelayMs = (int)HideDelay.Value,
        FlowAnimationEnabled = FlowEnabled.IsChecked == true,
        FlowDurationMs = (int)FlowDuration.Value,
    };

    /// <summary>Gibt die neuen Einstellungen und den gewünschten Autostart zurück, oder null bei Abbruch.</summary>
    public static (AppSettings Settings, bool Autostart)? Edit(Window? owner, AppSettings settings, bool autostart)
    {
        var dialog = new SettingsDialog(settings, autostart);
        return dialog.ShowFor(owner) == true ? (dialog.Result, dialog.Autostart.IsChecked == true) : null;
    }

    private void Apply(AppSettings settings)
    {
        ShowDelay.Value = settings.ShowDelayMs;
        HideDelay.Value = settings.HideDelayMs;
        FlowEnabled.IsChecked = settings.FlowAnimationEnabled;
        FlowDuration.Value = settings.FlowDurationMs;
    }

    private void Defaults_Click(object sender, RoutedEventArgs e) => Apply(new AppSettings());

    private void Save_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
