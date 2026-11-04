using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BiMaDock.Core.Settings;
using BiMaDock.Services;

namespace BiMaDock.Views;

public partial class SettingsDialog : DialogWindow
{
    private static readonly string[] DockPresets = ["#2A2A30", "#1A1A1D", "#3A3A42", "#1F2A44", "#0F3D4C", "#1E3A2B", "#3A2A3F", "#4A2B22", "#E8E8EC"];
    private static readonly string[] BorderPresets = ["#FFFFFF", "#000000", "#4CC2FF", "#FF8C00", "#6CCB5F", "#B388FF"];
    private static readonly string[] FeedbackPresets = ["#D2691E", "#2A7FB8", "#2E7D32", "#8E24AA", "#C62828", "#546E7A", "#F9A825"];
    private static readonly string[] AccentPresets = ["#4CC2FF", "#FF8C00", "#6CCB5F", "#FF5C8A", "#B388FF", "#FFD54F", "#E74C3C", "#FFFFFF"];

    private static readonly (HoverEffect Value, string Label)[] HoverChoices =
    [
        (HoverEffect.None, "Keine"),
        (HoverEffect.Zoom, "Vergrößern"),
        (HoverEffect.Hop, "Hüpfen"),
        (HoverEffect.Swing, "Schaukeln"),
        (HoverEffect.Wiggle, "Wackeln"),
        (HoverEffect.Pulse, "Pulsieren"),
    ];

    private static readonly (SlideStyle Value, string Label)[] SlideChoices =
    [
        (SlideStyle.None, "Sofort"),
        (SlideStyle.Glide, "Gleiten"),
        (SlideStyle.Spring, "Gleiten mit Federn"),
    ];

    private readonly ColorField _dockColor;
    private readonly ColorField _borderColor;
    private readonly ColorField _accentColor;
    private readonly ColorField _feedbackColor;
    private readonly ChipGroup<HoverEffect> _hoverEffect;
    private readonly ChipGroup<SlideStyle> _slideStyle;
    private readonly IconService _icons;
    private readonly Action<AppSettings> _preview;
    private HoverTuningMap _tunings = [];
    private bool _loading;

    private SettingsDialog(AppSettings settings, bool autostart, IconService icons, Action<AppSettings> preview)
    {
        _preview = preview;
        _icons = icons;
        InitializeComponent();

        _dockColor = new ColorField(DockPresets, settings.DockColor);
        _borderColor = new ColorField(BorderPresets, settings.BorderColor);
        _accentColor = new ColorField(AccentPresets, settings.AccentColor);
        _feedbackColor = new ColorField(FeedbackPresets, settings.FeedbackColor);
        DockColorHost.Content = _dockColor;
        BorderColorHost.Content = _borderColor;
        AccentColorHost.Content = _accentColor;
        FeedbackColorHost.Content = _feedbackColor;

        _hoverEffect = new ChipGroup<HoverEffect>(HoverEffects, HoverChoices, ShowHoverValues);
        _slideStyle = new ChipGroup<SlideStyle>(SlideStyles, SlideChoices, Preview);

        Apply(settings);
        Autostart.IsChecked = autostart;
        UpdateLibraryInfo();

        foreach (var field in new[] { _dockColor, _borderColor, _accentColor, _feedbackColor })
        {
            field.ValueChanged += (_, _) => Preview();
        }

        HoverIntensity.ValueChanged += (_, _) => StoreHoverValues();
        HoverSpeed.ValueChanged += (_, _) => StoreHoverValues();

        foreach (var slider in new[] { DockOpacity, ShowDelay, HideDelay, FlowDuration, SlideDuration, SlideSpring, MagnifyScale, MagnifyRange, MagnifySpeed, LaunchBounceHeight, LaunchBounceCount, LaunchBounceDuration, ItemPopDuration, ItemPopOvershoot })
        {
            slider.ValueChanged += (_, _) => Preview();
        }

        foreach (var box in new[] { ShowLabels, FlowEnabled, MagnifyEnabled, LaunchBounce, ItemPopEnabled })
        {
            box.Click += (_, _) => Preview();
        }
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
        HoverEffect = _hoverEffect.Value,
        HoverTunings = new(_tunings),
        LaunchBounce = LaunchBounce.IsChecked == true,
        LaunchBounceHeight = (int)LaunchBounceHeight.Value,
        LaunchBounceCount = (int)LaunchBounceCount.Value,
        LaunchBounceDurationMs = (int)LaunchBounceDuration.Value,
        MagnifySpeedMs = (int)MagnifySpeed.Value,
        SlideSpring = (int)SlideSpring.Value,
        ItemPopOvershoot = (int)ItemPopOvershoot.Value,
        MagnifyEnabled = MagnifyEnabled.IsChecked == true,
        MagnifyScale = (int)MagnifyScale.Value,
        MagnifyRange = (int)MagnifyRange.Value,
        SlideStyle = _slideStyle.Value,
        SlideDurationMs = (int)SlideDuration.Value,
        ItemPopEnabled = ItemPopEnabled.IsChecked == true,
        ItemPopDurationMs = (int)ItemPopDuration.Value,
    };

    /// <summary>
    /// Gibt die neuen Einstellungen und den gewünschten Autostart zurück, oder null bei Abbruch.
    /// <paramref name="preview"/> wird bei jeder Änderung aufgerufen; bei Abbruch mit den alten Werten.
    /// </summary>
    public static (AppSettings Settings, bool Autostart)? Edit(Window? owner, AppSettings settings, bool autostart, IconService icons, Action<AppSettings> preview)
    {
        var dialog = new SettingsDialog(settings, autostart, icons, preview);
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
        _hoverEffect.Value = settings.HoverEffect;
        _tunings = new(Enum.GetValues<HoverEffect>().ToDictionary(e => e, settings.Hover));
        LoadHoverValues();
        LaunchBounce.IsChecked = settings.LaunchBounce;
        LaunchBounceHeight.Value = settings.LaunchBounceHeight;
        LaunchBounceCount.Value = settings.LaunchBounceCount;
        LaunchBounceDuration.Value = settings.LaunchBounceDurationMs;
        MagnifySpeed.Value = settings.MagnifySpeedMs;
        SlideSpring.Value = settings.SlideSpring;
        ItemPopOvershoot.Value = settings.ItemPopOvershoot;
        MagnifyEnabled.IsChecked = settings.MagnifyEnabled;
        MagnifyScale.Value = settings.MagnifyScale;
        MagnifyRange.Value = settings.MagnifyRange;
        _slideStyle.Value = settings.SlideStyle;
        SlideDuration.Value = settings.SlideDurationMs;
        ItemPopEnabled.IsChecked = settings.ItemPopEnabled;
        ItemPopDuration.Value = settings.ItemPopDurationMs;
        _loading = false;
        Preview();
    }

    private void Preview()
    {
        if (_loading)
        {
            return;
        }

        // Nur die Regler der gewählten Animation zeigen.
        SlideDuration.IsEnabled = _slideStyle.Value != SlideStyle.None;
        SpringPanel.Visibility = _slideStyle.Value == SlideStyle.Spring ? Visibility.Visible : Visibility.Collapsed;
        bool zoomByMagnify = _hoverEffect.Value == HoverEffect.Zoom && MagnifyEnabled.IsChecked == true;
        ZoomHint.Visibility = zoomByMagnify ? Visibility.Visible : Visibility.Collapsed;
        HoverPanel.Visibility = _hoverEffect.Value == HoverEffect.None || zoomByMagnify ? Visibility.Collapsed : Visibility.Visible;
        _preview(Result.Normalized());
    }

    /// <summary>Effekt gewechselt: dessen eigene Werte in die Regler laden.</summary>
    private void ShowHoverValues()
    {
        bool wasLoading = _loading;
        _loading = true;
        LoadHoverValues();
        _loading = wasLoading;
        Preview();
    }

    private void LoadHoverValues()
    {
        var tuning = _tunings.GetValueOrDefault(_hoverEffect.Value) ?? new HoverTuning();
        HoverIntensity.Value = tuning.Intensity;
        HoverSpeed.Value = tuning.Speed;
        string name = HoverChoices.First(c => c.Value == _hoverEffect.Value).Label;
        HoverValuesTitle.Text = $"Werte für „{name}“, jeder Effekt merkt sich seine eigenen.";
    }

    private void StoreHoverValues()
    {
        if (_loading)
        {
            return;
        }

        _tunings[_hoverEffect.Value] = new HoverTuning { Intensity = (int)HoverIntensity.Value, Speed = (int)HoverSpeed.Value };
        Preview();
    }

    private void UpdateLibraryInfo()
    {
        var icons = _icons.Library.Load();
        int own = icons.Count(i => !i.IsBuiltIn);
        int categories = icons.Where(i => i.IsBuiltIn).Select(i => i.Category).Distinct().Count();
        LibraryInfo.Text = $"{icons.Count - own} mitgelieferte Symbole in {categories} Kategorien, {own} eigene.";
    }

    private void OpenLibrary_Click(object sender, RoutedEventArgs e)
    {
        IconPickerDialog.Manage(this, _icons);
        UpdateLibraryInfo();
    }

    private void OpenUserIcons_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_icons.Library.UserDirectory);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_icons.Library.UserDirectory}\"") { UseShellExecute = true });
    }

    private void Defaults_Click(object sender, RoutedEventArgs e) => Apply(new AppSettings());

    private void Save_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    /// <summary>Auswahl als Reihe von Schaltflächen ("Chips"); genau einer ist markiert.</summary>
    private sealed class ChipGroup<T>
        where T : struct, Enum
    {
        private readonly Panel _panel;
        private T _value;

        public ChipGroup(Panel panel, IEnumerable<(T Value, string Label)> choices, Action changed)
        {
            _panel = panel;
            foreach (var (value, label) in choices)
            {
                var chip = new Border
                {
                    Tag = value,
                    CornerRadius = new CornerRadius(6),
                    BorderThickness = new Thickness(1.5),
                    Padding = new Thickness(10, 5, 10, 5),
                    Margin = new Thickness(0, 0, 6, 6),
                    Cursor = Cursors.Hand,
                    Child = new TextBlock { Text = label, Foreground = Brushes.White },
                };
                chip.MouseLeftButtonDown += (_, e) =>
                {
                    e.Handled = true;
                    Value = value;
                    changed();
                };
                panel.Children.Add(chip);
            }
        }

        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                var accent = (Brush)_panel.FindResource("AccentBrush");
                foreach (Border chip in _panel.Children)
                {
                    bool selected = EqualityComparer<T>.Default.Equals((T)chip.Tag, value);
                    chip.BorderBrush = selected ? accent : new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));
                    chip.Background = selected ? new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)) : Brushes.Transparent;
                }
            }
        }
    }
}
