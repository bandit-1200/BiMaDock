using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using System;
using System.IO;
using Microsoft.Win32; // Für SystemEvents

namespace BiMaDock
{
    public partial class MainWindow : Window
    {

        private DispatcherTimer timer;

        private GlobalMouseHook? mouseHook;
        private DockManager dockManager;
        private readonly DockNavigationController mainDockNavigation;
        private readonly DockNavigationController categoryDockNavigation;

        public bool dockVisible = true;
        public bool isDragging = false; // Flag für Dragging
        private DispatcherTimer dockHideTimer;
        private DispatcherTimer categoryHideTimer;

        private string currentOpenCategory = "";
        public bool isCategoryDockOpen = false;
        public string isCategoryDockOpenID = "";

        [Flags]
        public enum DockStatus
        {
            None = 0,
            MainDockHover = 1,
            CategoryDockHover = 2,
            ContextMenuOpen = 4,
            CategoryElementClicked = 8,
            DraggingToDock = 16,
            EditSettingsDock = 32

        }

        public DockStatus currentDockStatus = DockStatus.None;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                File.AppendAllText(AppPaths.GetLogFilePath(), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] MainWindow constructor after InitializeComponent{Environment.NewLine}");

                mainDockNavigation = new DockNavigationController(
                    MainDockScrollViewer, MainDockPreviousButton, MainDockNextButton);
                categoryDockNavigation = new DockNavigationController(
                    CategoryDockScrollViewer, CategoryDockPreviousButton, CategoryDockNextButton);
                CheckAutostart();

                this.SizeChanged += MainWindow_SizeChanged;
                SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
                CenterWindow();
                this.WindowStyle = WindowStyle.None;
                this.ResizeMode = ResizeMode.NoResize;
                this.Topmost = true;

                this.StateChanged += MainWindow_StateChanged;

                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(20);
                timer.Tick += CheckForRdpFullScreen;
                timer.Start();

                ButtonAnimations.LoadSettings();
                SettingsWindow.ApplyStyleSettings(this);
                AllowDrop = true;
                Debug.WriteLine("Hauptfenster initialisiert.");
                dockManager = new DockManager(DockPanel, CategoryDockContainer, this);
                dockManager.LoadDockItems();
                RefreshDockNavigation();
                Debug.WriteLine("Dock-Elemente geladen.");

                dockHideTimer = new DispatcherTimer();
                dockHideTimer.Interval = TimeSpan.FromSeconds(0.3);
                dockHideTimer.Tick += (s, e) => { HideDock(); };

                categoryHideTimer = new DispatcherTimer();
                categoryHideTimer.Interval = TimeSpan.FromSeconds(0.3);
                categoryHideTimer.Tick += (s, e) => { HideCategoryDockPanel(); };

                this.Closing += (s, e) =>
                {
                    timer.Stop();
                    mouseHook?.Unhook();
                    mouseHook = null;

                    string currentCategory = "";
                    dockManager.SaveDockItems(currentCategory);
                };

                DockPanel.DragEnter += DockPanel_DragEnter;
                DockPanel.DragLeave += DockPanel_DragLeave;
                DockPanel.Drop += dockManager.DockPanel_Drop;

                this.Loaded += (s, e) =>
                {
                    RefreshDockNavigation();

                    if (mouseHook == null)
                    {
                        mouseHook = new GlobalMouseHook(this);
                    }

                    CenterWindow();

                    // Erst nach dem ersten Layout ist die Dock-Höhe bekannt; das Ausblenden im Konstruktor
                    // hätte das Dock sonst sichtbar gelassen.
                    if (!IsMouseOver)
                    {
                        dockVisible = true;
                        HideDock();
                    }
                };

                DockPanel.MouseRightButtonDown += (s, e) =>
                {
                    OpenMenuItem.Visibility = Visibility.Collapsed;
                    DeleteMenuItem.Visibility = Visibility.Collapsed;
                    EditMenuItem.Visibility = Visibility.Collapsed;
                    FindFileItem.Visibility = Visibility.Collapsed;
                    DockContextMenu.IsOpen = true;
                    if (DockContextMenu.IsOpen)
                    {
                        currentDockStatus |= DockStatus.ContextMenuOpen;
                        CheckAllConditions();
                    }
                };

                CategoryDockContainer.AllowDrop = true;

                HideCategoryDockPanel();
                Debug.WriteLine("MainWindow: HideCategoryDockPanel");
                HideDock();
                Debug.WriteLine("MainWindow: HideDock");
                UpdateCheck();
                Debug.WriteLine("MainWindow: UpdateCheck");
                File.AppendAllText(AppPaths.GetLogFilePath(), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] MainWindow constructor complete{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                var logPath = AppPaths.GetLogFilePath();
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] MainWindow constructor exception: {ex}{Environment.NewLine}");
                throw;
            }
        }

        public void CheckAllConditions()
        {

            if (currentDockStatus > 0) // Überprüft, ob irgendein Flag gesetzt ist
            {
                categoryHideTimer.Stop();
                dockHideTimer.Stop();
            }
            else
            {
                categoryHideTimer.Start();
                dockHideTimer.Start();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            timer?.Stop();
            mouseHook?.Unhook();
            mouseHook = null;
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged; // Event abmelden, um Speicherlecks zu vermeiden
            base.OnClosed(e);
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CenterWindow();
        }

        private void CenterWindow()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            UpdateDockScrollWidths(screenWidth);

            double windowWidth = ActualWidth > 0 ? ActualWidth : Width;

            this.Left = (screenWidth - windowWidth) / 2;
            this.Top = 0;
        }

        private void UpdateDockScrollWidths(double screenWidth)
        {
            double mainDockFixedWidth = MainDockPreviousButton.ActualWidth + MainDockNextButton.ActualWidth
                + MainDockBorder.BorderThickness.Left + MainDockBorder.BorderThickness.Right
                + MainDockBorder.Padding.Left + MainDockBorder.Padding.Right;
            double categoryDockFixedWidth = CategoryDockPreviousButton.ActualWidth + CategoryDockNextButton.ActualWidth
                + CategoryDockBorder.BorderThickness.Left + CategoryDockBorder.BorderThickness.Right
                + CategoryDockBorder.Padding.Left + CategoryDockBorder.Padding.Right;

            MainDockScrollViewer.MaxWidth = Math.Max(0, screenWidth - mainDockFixedWidth);
            CategoryDockScrollViewer.MaxWidth = Math.Max(0, screenWidth - categoryDockFixedWidth);
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            CenterWindow(); // Fenster neu zentrieren
        }
    }

}
