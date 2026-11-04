using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BiMaDock.Core.Settings;
using BiMaDock.Services;

namespace BiMaDock.Views;

/// <summary>
/// Farbauswahl: Reihe vorgegebener Farbfelder plus Eingabefeld für eigene Werte (#RRGGBB).
/// </summary>
internal sealed class ColorField : StackPanel
{
    private readonly TextBox _hexBox;
    private readonly Border _preview;
    private readonly List<Border> _swatches = [];
    private string _value;
    private bool _updating;

    public ColorField(IEnumerable<string> presets, string initial)
    {
        _value = AppSettings.NormalizeColor(initial, initial);
        Orientation = Orientation.Horizontal;
        Margin = new Thickness(0, 2, 0, 0);

        foreach (var preset in presets)
        {
            var swatch = new Border
            {
                Width = 26,
                Height = 26,
                Margin = new Thickness(0, 0, 6, 0),
                CornerRadius = new CornerRadius(13),
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(ThemeService.Parse(preset)),
                Cursor = Cursors.Hand,
                Tag = preset,
                ToolTip = preset,
            };
            // Beim Drücken behandeln: sonst startet der Dialog das Verschieben des Fensters und schluckt den Klick.
            swatch.MouseLeftButtonDown += (_, e) =>
            {
                Value = (string)swatch.Tag;
                e.Handled = true;
            };
            _swatches.Add(swatch);
            Children.Add(swatch);
        }

        _preview = new Border
        {
            Width = 26,
            Height = 26,
            Margin = new Thickness(8, 0, 6, 0),
            CornerRadius = new CornerRadius(6),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
        };
        Children.Add(_preview);

        _hexBox = new TextBox { Width = 92, Style = (Style)Application.Current.FindResource("DialogTextBoxStyle") };
        _hexBox.TextChanged += (_, _) =>
        {
            if (_updating)
            {
                return;
            }

            string normalized = AppSettings.NormalizeColor(_hexBox.Text, string.Empty);
            if (normalized.Length > 0)
            {
                SetValue(normalized, updateText: false);
            }
        };
        Children.Add(_hexBox);

        SetValue(_value, updateText: true);
    }

    public event EventHandler? ValueChanged;

    public string Value
    {
        get => _value;
        set => SetValue(AppSettings.NormalizeColor(value, _value), updateText: true);
    }

    private void SetValue(string value, bool updateText)
    {
        bool changed = !string.Equals(_value, value, StringComparison.OrdinalIgnoreCase);
        _value = value;
        _preview.Background = new SolidColorBrush(ThemeService.Parse(value));
        foreach (var swatch in _swatches)
        {
            bool selected = string.Equals((string)swatch.Tag, value, StringComparison.OrdinalIgnoreCase);
            swatch.BorderBrush = selected ? Brushes.White : new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));
        }

        if (updateText)
        {
            _updating = true;
            _hexBox.Text = value;
            _updating = false;
        }

        if (changed)
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
