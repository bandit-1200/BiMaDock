using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System;

namespace BiMaDock
{
    public partial class MainWindow
    {
        private DispatcherTimer? showDockTimer; // neu: Verzögerung beim Einblenden (nullable)

        private void MainDockArea_MouseEnter(object sender, MouseEventArgs e)
        {
            StartShowDelay();
            dockHideTimer?.Stop();
            categoryHideTimer?.Stop();
        }

        private void MainDockArea_MouseLeave(object sender, MouseEventArgs e)
        {
            CancelShowDelay();
            currentDockStatus &= ~DockStatus.MainDockHover;
            CheckAllConditions();
            dockHideTimer?.Start();
        }

        public int DockShowDelayMilliseconds { get; private set; } = 300;

        public void SetDockShowDelayMilliseconds(int milliseconds)
        {
            DockShowDelayMilliseconds = Math.Clamp(milliseconds, 100, 1000);
        }

        public void ShowDock()
        {
            mouseHook?.SetHook();
            if (!dockVisible)
            {
                dockVisible = true;
                var duration = TimeSpan.FromMilliseconds(100);
                var slideAnimation = new ThicknessAnimation
                {
                    From = new Thickness(0, -MainDockBorder.ActualHeight + 5, 0, 0),
                    To = new Thickness(0, 0, 0, 0),
                    Duration = duration,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
                    FillBehavior = FillBehavior.HoldEnd
                };
                slideAnimation.Completed += (s, e) =>
                {
                    MainDockBorder.Margin = new Thickness(0);
                };



                MainDockBorder.BeginAnimation(FrameworkElement.MarginProperty, slideAnimation);
            }
        }






        public void HideDock()
        {
            // Der Timer hat seine Aufgabe erfüllt; sonst würde er alle 0,3 s weiter feuern.
            dockHideTimer?.Stop();
            mouseHook?.Unhook();
            HideCategoryDockPanel();
            currentDockStatus = DockStatus.None;
            if (dockVisible)
            {
                dockVisible = false;
                var duration = TimeSpan.FromMilliseconds(100);
                var toValue = -MainDockBorder.ActualHeight + 5;
                var slideAnimation = new ThicknessAnimation
                {
                    From = new Thickness(0, 0, 0, 0),
                    To = new Thickness(0, toValue, 0, 0),
                    Duration = duration,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
                    FillBehavior = FillBehavior.HoldEnd
                };

                slideAnimation.Completed += (s, e) =>
                {
                    MainDockBorder.Margin = new Thickness(0, toValue, 0, 0);
                };



                MainDockBorder.BeginAnimation(FrameworkElement.MarginProperty, slideAnimation);
            }
            isCategoryDockOpen = false;

        }

        // --- Einfügen: StartShowDelay / CancelShowDelay (behebt CS0103) ---
        // Startet verzögertes Einblenden des Docks (aufrufbar von DockManager)
        public void StartShowDelay(int? milliseconds = null)
        {
            // UI-Thread verwenden
            Dispatcher.Invoke(() =>
            {
                if (showDockTimer == null)
                {
                    showDockTimer = new DispatcherTimer();
                    showDockTimer.Tick += (s, e) =>
                    {
                        try
                        {
                            showDockTimer.Stop();
                            if (!isDragging)
                            {
                                ShowDock();
                                currentDockStatus |= DockStatus.MainDockHover;
                                CheckAllConditions();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"StartShowDelay: {ex.Message}");
                        }
                    };
                }

                var delay = milliseconds ?? DockShowDelayMilliseconds;
                showDockTimer.Interval = TimeSpan.FromMilliseconds(Math.Clamp(delay, 100, 1000));
                showDockTimer.Stop();
                showDockTimer.Start();
            });
        }

        // Bricht ein geplantes verzögertes Einblenden ab
        public void CancelShowDelay()
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    showDockTimer?.Stop();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CancelShowDelay: {ex.Message}");
                }
            });
        }
    }
}
