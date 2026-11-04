using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BiMaDock.Core.Settings;

namespace BiMaDock.Views;

public partial class SettingsDialog : DialogWindow
{
    private static readonly string[] DockPresets = ["#2A2A30", "#1A1A1D", "#3A3A42", "#1F2A44", "#0F3D4C", "#1E3A2B", "#3A2A3F", "#4A2B22", "#E8E8EC"];
    private static readonly string[] BorderPresets = ["#FFFFFF", "#000000", "#4CC2FF", "#FF8C00", "#6CCB5F", "#B388FF"];
    private static readonly string[] FeedbackPresets = ["#D2691E", "#2A7FB8", "#2E7D32", "#8E24AA", "#C62828", "#546E7A", "#F9A825"];
    private static readonly string[] AccentPresets = ["#4CC2FF", "#FF8C00", "#6CCB5F", "#FF5C8A", "#B388FF", "#FFD54F", "#E74C3C", "#FFFFFF"];

    private readonly ColorField _dockColor;
    private readonly ColorField _borderColor;
    private readonly ColorField _accentColor;
    private readonly ColorField _feedbackColor;
    private static readonly (HoverEffect Effect, string Label)[] Effects =
    [
        (HoverEffect.None, "Keine"),
        (HoverEffect.Zoom, "Vergrößern"),
        (HoverEffect.Hop, "Hüpfen"),
        (HoverEffect.Swing, "Schaukeln"),
        (HoverEffect.Wiggle, "Wackeln"),
        (HoverEffect.Pulse, "Pulsieren"),
    ];

    private readonly Action<AppSettings> _preview;
    private HoverEffect _hoverEffect;
    private bool _loading;

    private SettingsDialog(AppSettings settings, bool autostart, Action<AppSettings> preview)
    {
        _preview = preview;
        InitializeComponent();

        _dockColor = new ColorField(DockPresets, settings.DockColor);
        _borderColor = new ColorField(BorderPresets, settings.BorderColor);
        _accentColor = new ColorField(AccentPresets, settings.AccentColor);
        DockColorHost.Content = _dockColor;
        BorderColorHost.Content = _borderColor;
        AccentColorHost.Content = _accentColor;
        _feedbackColor = new ColorField(FeedbackPresets, settings.FeedbackColor);
        FeedbackColorHost.Content = _feedbackColor;

        foreach (var (effect, label) in Effects)
        {
            var chip = new Border
            {
                Tag = effect,
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1.5),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 6, 6),
                Cursor = System.Windows.Input.Cursors.Hand,
                Child = new TextBlock { Text = label, Foreground = Brushes.White },
            };
            chip.MouseLeftButtonDown += (_, e) =>
            {
                e.Handled = true;
                SelectEffect(effect);
                Preview();
            };
            HoverEffects.Children.Add(chip);
        }

        Apply(settings);
        Autostart.IsChecked = autostart;

        foreach (var field in new[] { _dockColor, _borderColor, _accentColor, _feedbackColor })
        {
            field.ValueChanged += (_, _) => Preview();
        }

        DockOpacity.ValueChanged += (_, _) => Preview();
        ShowLabels.Click += (_, _) => Preview();
        LaunchBounce.Click += (_, _) => Preview();
    }

    private AppSettings Result => new()
    {
        ShowDelayMs = (int)ShowDelay.Value,
        HideDelayMs = (int)HideDelay.Value,
        FlowAnimationEnabled = FlowEnabled.IsChecked == true,
        FlowDurationMs = (int)FlowDuration.Value,
        DockColor = _dockColor.Value,
        DockOpacity = (int)DockOpacity.Value,
        BorderColor = _borderColor.Value,
        AccentColor = _accentColor.Value,
        FeedbackColor = _feedbackColor.Value,
        ShowLabels = ShowLabels.IsChecked == true,
        HoverEffect = _hoverEffect,
        LaunchBounce = LaunchBounce.IsChecked == true,
    };

    /// <summary>
    /// Gibt die neuen Einstellungen und den gewünschten Autostart zurück, oder null bei Abbruch.
    /// <paramref name="preview"/> wird bei jeder Farbänderung aufgerufen; bei Abbruch mit den alten Werten.
    /// </summary>
    public static (AppSettings Settings, bool Autostart)? Edit(Window? owner, AppSettings settings, bool autostart, Action<AppSettings> preview)
    {
        var dialog = new SettingsDialog(settings, autostart, preview);
        if (dialog.ShowFor(owner) == true)
        {
            return (dialog.Result.Normalized(), dialog.Autostart.IsChecked == true);
        }

        preview(settings);
        return null;
    }

    private void Apply(AppSettings settings)
    {
        _loading = true;
        ShowDelay.Value = settings.ShowDelayMs;
        HideDelay.Value = settings.HideDelayMs;
        FlowEnabled.IsChecked = settings.FlowAnimationEnabled;
        FlowDuration.Value = settings.FlowDurationMs;
        DockOpacity.Value = settings.DockOpacity;
        _dockColor.Value = settings.DockColor;
        _borderColor.Value = settings.BorderColor;
        _accentColor.Value = settings.AccentColor;
        _feedbackColor.Value = settings.FeedbackColor;
        ShowLabels.IsChecked = settings.ShowLabels;
        LaunchBounce.IsChecked = settings.LaunchBounce;
        SelectEffect(settings.HoverEffect);
        _loading = false;
        Preview();
    }

    private void SelectEffect(HoverEffect effect)
    {
        _hoverEffect = effect;
        var accent = (Brush)FindResource("AccentBrush");
        foreach (Border chip in HoverEffects.Children)
        {
            bool selected = (HoverEffect)chip.Tag == effect;
            chip.BorderBrush = selected ? accent : new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));
            chip.Background = selected ? new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)) : Brushes.Transparent;
        }
    }

    private void Preview()
    {
        if (!_loading)
        {
            _preview(Result.Normalized());
        }
    }

    private void Defaults_Click(object sender, RoutedEventArgs e) => Apply(new AppSettings());

    private void Save_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
