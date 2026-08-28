using System;
using System.Windows;
using System.Windows.Controls;

namespace BiMaDock;

/// <summary>
/// Verwaltet die Pfeilnavigation eines horizontalen Docks.
/// </summary>
internal sealed class DockNavigationController
{
    private const double ItemScrollStep = 80;

    private readonly ScrollViewer scrollViewer;
    private readonly Button previousButton;
    private readonly Button nextButton;

    public DockNavigationController(ScrollViewer scrollViewer, Button previousButton, Button nextButton)
    {
        this.scrollViewer = scrollViewer ?? throw new ArgumentNullException(nameof(scrollViewer));
        this.previousButton = previousButton ?? throw new ArgumentNullException(nameof(previousButton));
        this.nextButton = nextButton ?? throw new ArgumentNullException(nameof(nextButton));
    }

    public void ScrollPrevious() => ScrollBy(-ItemScrollStep);

    public void ScrollNext() => ScrollBy(ItemScrollStep);

    public void Refresh()
    {
        bool hasOverflow = scrollViewer.ScrollableWidth > 0;
        Visibility visibility = hasOverflow ? Visibility.Visible : Visibility.Collapsed;

        SetButtonState(previousButton, visibility, hasOverflow && scrollViewer.HorizontalOffset > 0);
        SetButtonState(nextButton, visibility, hasOverflow && scrollViewer.HorizontalOffset < scrollViewer.ScrollableWidth);
    }

    private void ScrollBy(double delta)
    {
        scrollViewer.ScrollToHorizontalOffset(Math.Clamp(
            scrollViewer.HorizontalOffset + delta, 0, scrollViewer.ScrollableWidth));
    }

    private static void SetButtonState(Button button, Visibility visibility, bool isEnabled)
    {
        if (button.Visibility != visibility)
        {
            button.Visibility = visibility;
        }

        if (button.IsEnabled != isEnabled)
        {
            button.IsEnabled = isEnabled;
        }
    }
}
