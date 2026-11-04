using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BiMaDock.Views;

/// <summary>Lupe wie beim Mac: Symbole unter der Maus wachsen, Nachbarn wachsen etwas mit.</summary>
public partial class DockWindow
{
    private DateTime _magnifyEnteredAt;

    private void HookMagnify(ItemsControl items)
    {
        items.MouseEnter += (_, e) =>
        {
            _magnifyEnteredAt = DateTime.UtcNow;
            UpdateMagnify(items, e, smooth: true);
        };
        items.MouseMove += (_, e) =>
        {
            // In der kurzen Einblendphase läuft die weiche Animation weiter.
            bool smooth = DateTime.UtcNow - _magnifyEnteredAt < TimeSpan.FromMilliseconds(110);
            UpdateMagnify(items, e, smooth);
        };
        items.MouseLeave += (_, _) => ItemAnimations.ResetMagnify(ItemButtons(items), animate: true);
    }

    private void UpdateMagnify(ItemsControl items, MouseEventArgs e, bool smooth)
    {
        if (!ItemAnimations.Settings.MagnifyEnabled || IsDragActive || e.LeftButton == MouseButtonState.Pressed)
        {
            return;
        }

        ItemAnimations.Magnify(ItemButtons(items), items, e.GetPosition(items).X, smooth);
    }

    /// <summary>Lupe sofort zurücksetzen, z. B. bevor ein Symbol gezogen wird (Lücke braucht feste Größen).</summary>
    private void ResetMagnifyNow()
    {
        ItemAnimations.ResetMagnify(ItemButtons(MainItemsControl), animate: false);
        ItemAnimations.ResetMagnify(ItemButtons(CategoryItemsControl), animate: false);
        UpdateLayout();
    }

    private static List<Button> ItemButtons(ItemsControl items)
    {
        var buttons = new List<Button>(items.Items.Count);
        foreach (object item in items.Items)
        {
            if (items.ItemContainerGenerator.ContainerFromItem(item) is DependencyObject container && FindButton(container) is { } button)
            {
                buttons.Add(button);
            }
        }

        return buttons;
    }

    private static Button? FindButton(DependencyObject parent)
    {
        if (parent is Button button)
        {
            return button;
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            if (FindButton(VisualTreeHelper.GetChild(parent, i)) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>Button eines Eintrags in Haupt- oder Kategorie-Dock.</summary>
    private Button? ButtonOf(ViewModels.DockItemViewModel item) =>
        (MainItemsControl.ItemContainerGenerator.ContainerFromItem(item) ?? CategoryItemsControl.ItemContainerGenerator.ContainerFromItem(item))
            is DependencyObject container ? FindButton(container) : null;
}
