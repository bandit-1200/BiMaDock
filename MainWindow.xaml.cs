using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Collections;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Win32; // Für SystemEvents

namespace BiMaDock
{
    public partial class MainWindow : Window
    {

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private DispatcherTimer timer;
        private IntPtr lastForegroundWindow = IntPtr.Zero;

        [StructLayout(LayoutKind.Sequential)]
        struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        const int SW_MAXIMIZE = 3;
        const int SW_MINIMIZE = 6;

        private GlobalMouseHook mouseHook;
        private DockManager dockManager;

        public bool dockVisible = true;
        public bool IsDragging => isDragging;
        private Point? dragStartPoint = null;
        private Button? draggedButton = null;
        public bool isDragging = false; // Flag für Dragging
        private DispatcherTimer dockHideTimer;
        private DispatcherTimer categoryHideTimer;

        private string currentOpenCategory = "";
        public bool isCategoryDockOpen = false;
        public string isCategoryDockOpenID = "";

        private bool isCategoryMessageShown = false;

        private Border? currentPlaceholder = null; // Referenz auf den Platzhalter
        private int currentPlaceholderIndex = -1; // Index des Platzhalters, der die aktuelle Position speichert

        public void SetDragging(bool value)
        {
            isDragging = value;
        }

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
        private Dictionary<Button, bool> animationPlayed = new Dictionary<Button, bool>();
        private Point? previousCategoryMousePosition = null;



        public MainWindow()
        {
            InitializeComponent();
            CheckAutostart();

            this.SizeChanged += MainWindow_SizeChanged;     // Event abonnieren, um auf Änderungen der Fenstergröße zu reagieren
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged; // Event abonnieren, um auf Änderungen der Bildschirmauflösung zu reagieren
            CenterWindow();     // Initiale Zentrierung des Fensters
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;
            this.Topmost = true;

            this.StateChanged += MainWindow_StateChanged;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1); // Überprüfe jede Sekunde
            timer.Tick += CheckForRdpFullScreen;
            timer.Start();

            // GlobalMouseHook.SetHook();
            mouseHook = new GlobalMouseHook(this);

            //  SettingsWindow.LoadSettings();
            SettingsWindow settingsWindow = new SettingsWindow(this);
            double screenWidth = SystemParameters.PrimaryScreenWidth;

            ButtonAnimations.LoadSettings(); //Animation
            settingsWindow.LoadSettings(); // Einstellungen laden
            AllowDrop = true;
            Debug.WriteLine("Hauptfenster initialisiert."); // Debugging
            dockManager = new DockManager(DockPanel, CategoryDockContainer, this); // Übergeben von CategoryDockContainer
            dockManager.LoadDockItems();
            Debug.WriteLine("Dock-Elemente geladen."); // Debugging


            // Timer initialisieren
            dockHideTimer = new DispatcherTimer();
            dockHideTimer.Interval = TimeSpan.FromSeconds(0.3); // Zeitintervall auf 5 Sekunden setzen
            dockHideTimer.Tick += (s, e) => { HideDock(); };

            // Timer initialisieren
            categoryHideTimer = new DispatcherTimer();
            categoryHideTimer.Interval = TimeSpan.FromSeconds(0.3); // Zeitintervall auf 5 Sekunden setzen
            categoryHideTimer.Tick += (s, e) => { HideCategoryDockPanel(); };

            this.Closing += (s, e) =>
            {
                // Sicherstellen, dass die aktuelle Kategorie übergeben wird
                string currentCategory = ""; // Hier die aktuelle Kategorie ermitteln
                dockManager.SaveDockItems(currentCategory);
            };

            DockPanel.PreviewMouseLeftButtonDown += DockPanel_MouseLeftButtonDown;
            DockPanel.PreviewMouseMove += DockPanel_MouseMove;
            DockPanel.PreviewMouseLeftButtonUp += DockPanel_MouseLeftButtonUp;
            // DockPanel.PreviewGiveFeedback += DockPanel_PreviewGiveFeedback; 
            DockPanel.DragEnter += DockPanel_DragEnter;
            DockPanel.DragLeave += DockPanel_DragLeave;
            DockPanel.Drop += dockManager.DockPanel_Drop;

            this.Loaded += (s, e) =>
            {
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var screenHeight = SystemParameters.PrimaryScreenHeight;
                this.Left = (screenWidth / 2) - (this.Width / 2);
                this.Top = 0; // Fenster am oberen Bildschirmrand positionieren
                this.MouseMove += CheckMousePosition; // Bestätigen, dass die Maus sich bewegt
                DockPanel.MouseEnter += DockPanel_MouseEnter;
                DockPanel.MouseLeave += DockPanel_MouseLeave;
                DockPanel.DragEnter += (s, e) =>
                {
                    e.Effects = DragDropEffects.All;
                    if (!dockVisible) ShowDock();
                };

                // Periodische Überprüfung, ob die Maus über einem der Docks ist
                var hoverCheckTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(0.2)
                };
                hoverCheckTimer.Tick += (s, e) => CheckMouseHover();
                hoverCheckTimer.Start();
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
                    // ShowDock(); // Dock sichtbar halten
                    currentDockStatus |= DockStatus.ContextMenuOpen;
                    CheckAllConditions();
                }
            };

            // Registriere die Event-Handler für das Kategoriedock
            CategoryDockContainer.AllowDrop = true;
            CategoryDockContainer.Drop -= CategoryDockContainer_Drop;
            CategoryDockContainer.Drop += CategoryDockContainer_Drop;
            CategoryDockContainer.DragEnter += CategoryDockContainer_DragEnter;
            CategoryDockContainer.DragLeave += CategoryDockContainer_DragLeave;
            CategoryDockContainer.PreviewMouseLeftButtonDown += CategoryDockContainer_PreviewMouseLeftButtonDown;
            CategoryDockContainer.MouseMove += CategoryDockContainer_MouseMove;





            var categoryDockContainer = this.FindName("CategoryDockContainer") as StackPanel;
            if (categoryDockContainer != null)
            {
                dockManager.InitializeCategoryDockContainer(categoryDockContainer);
            }
            else
            {
                Debug.WriteLine("CategoryDockContainer konnte nicht gefunden werden");
            }

            HideCategoryDockPanel();
            Debug.WriteLine("MainWindow: HideCategoryDockPanel");
            HideDock();
            Debug.WriteLine("MainWindow: HideDock");
            UpdateCheck();
            Debug.WriteLine("MainWindow: UpdateCheck");

        }
        // Weitere Initialisierung
        private void CheckForRdpFullScreen(object? sender, EventArgs e)
        {
            // Debug.WriteLine("CheckForRdpFullScreen: Überprüfe RDP-Sitzung...");

            bool rdpSessionActive = Process.GetProcessesByName("mstsc").Any();
            // Debug.WriteLine($"CheckForRdpFullScreen: RDP-Sitzung aktiv: {rdpSessionActive}");

            IntPtr foregroundWindow = GetForegroundWindow();
            RECT rect;

            if (GetWindowRect(foregroundWindow, out rect))
            {
                // Debug.WriteLine($"CheckForRdpFullScreen: Vordergrundfensterabmessungen: Links={rect.Left}, Oben={rect.Top}, Rechts={rect.Right}, Unten={rect.Bottom}");

                int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
                int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

                // Überprüfen, ob das Fenster maximiert ist
                if (IsWindowMaximized(foregroundWindow))
                {
                    // Debug.WriteLine("CheckForRdpFullScreen: Das Fenster ist maximiert.");
                    // Keine Änderung an Topmost, wenn das Fenster maximiert ist
                }

                // Überprüfen, ob das Fenster minimiert ist
                if (IsWindowMinimized(foregroundWindow))
                {
                    // Debug.WriteLine("CheckForRdpFullScreen: Das Fenster ist minimiert.");
                    if (!this.Topmost)
                    {
                        // Debug.WriteLine("CheckForRdpFullScreen: Setze Topmost auf True.");
                        this.Topmost = true;
                    }
                }

                if (rdpSessionActive)
                {
                    if (rect.Left == 0 && rect.Top == 0 && rect.Right == screenWidth && rect.Bottom == screenHeight)
                    {
                        // Debug.WriteLine("CheckForRdpFullScreen: RDP-Fenster ist im Vollbildmodus.");
                        if (this.Topmost)
                        {
                            // Debug.WriteLine("CheckForRdpFullScreen: Setze Topmost auf False.");
                            this.Topmost = false;
                        }
                    }
                    else
                    {
                        // Debug.WriteLine("CheckForRdpFullScreen: RDP-Fenster ist nicht im Vollbildmodus.");
                        if (this.Topmost)
                        {
                            // Debug.WriteLine("CheckForRdpFullScreen: Setze Topmost auf False.");
                            this.Topmost = false;
                        }
                    }
                }
                else
                {
                    // Debug.WriteLine("CheckForRdpFullScreen: RDP-Sitzung nicht aktiv.");
                    if (!this.Topmost)
                    {
                        // Debug.WriteLine("CheckForRdpFullScreen: Setze Topmost auf True.");
                        this.Topmost = true;
                    }
                }

                // Debug.WriteLine($"CheckForRdpFullScreen: Aktueller Status der Dockbar (Topmost): {this.Topmost}");
            }
            else
            {
                // Debug.WriteLine("CheckForRdpFullScreen: Konnte die Abmessungen des Vordergrundfensters nicht abrufen.");
                if (!this.Topmost)
                {
                    // Debug.WriteLine("CheckForRdpFullScreen: Setze Topmost auf True.");
                    this.Topmost = true;
                }
            }
        }

        private bool IsWindowMaximized(IntPtr hWnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            GetWindowPlacement(hWnd, ref placement);
            return placement.showCmd == SW_MAXIMIZE;
        }

        private bool IsWindowMinimized(IntPtr hWnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            GetWindowPlacement(hWnd, ref placement);
            return placement.showCmd == SW_MINIMIZE;
        }

        // Event-Handler für StateChanged Ereignis
        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            CheckForRdpFullScreen(sender, e);
        }


        public async Task ShowUpdateDialog()
        {
            string latestVersion = "1.2.3"; // Beispielversion
            string downloadUrl = await GetLatestReleaseDownloadUrlAsync(); // Hole den tatsächlichen Download-Link
            UpdateDialog updateDialog = new UpdateDialog(latestVersion, downloadUrl);
            updateDialog.ShowDialog();
        }


        public static async Task<string> GetLatestReleaseDownloadUrlAsync()
        {
            const string GitHubApiUrl = "https://api.github.com/repos/bandit-1200/BiMaDock/releases/latest";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "BiMaDock-Update-Checker");
                string response = await client.GetStringAsync(GitHubApiUrl);
                JObject releaseInfo = JObject.Parse(response);
                string downloadUrl = releaseInfo["assets"]?[0]?["browser_download_url"]?.ToString() ?? "Unknown";
                return downloadUrl;
            }
        }

        private async void TestUpdateDialogButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowUpdateDialog();
        }



        // Methode zum Öffnen des Einstellungsfensters
        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow(this);
            settingsWindow.ShowDialog(); // Modal anzeigen
        }



        private void Open_Click(object sender, RoutedEventArgs e)
        {
            dockManager.Open_Click(sender, e);
        }

        private void CheckMouseHover()
        {
            if (DockPanel != null && CategoryDockContainer != null)
            {
                var mousePosDock = Mouse.GetPosition(DockPanel);
                var dockBounds = new Rect(DockPanel.TranslatePoint(new Point(), this), DockPanel.RenderSize); // Grenzen des Haupt-Docks
                var mousePosCategory = Mouse.GetPosition(CategoryDockContainer);
                var categoryBounds = new Rect(CategoryDockContainer.TranslatePoint(new Point(), this), CategoryDockContainer.RenderSize); // Grenzen des Kategorie-Docks

                if (dockBounds.Contains(mousePosDock) || categoryBounds.Contains(mousePosCategory) || isDragging)
                {
                    // Timer stoppen, wenn die Maus über einem der Docks ist oder ein Draggen erkannt wird
                    dockHideTimer.Stop();
                    categoryHideTimer.Stop();
                    // Beide Docks sichtbar machen
                    ShowDock();
                    CategoryDockContainer.Visibility = Visibility.Visible;
                }
                else
                {
                    // Timer starten und Countdown für das Ausblenden der Docks starten
                    // dockHideTimer.Start();
                    // categoryHideTimer.Start();
                }
            }
        }

        private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("Mouse Down Event ausgelöst"); // Debugging
            isDragging = true; // Dragging-Flag setzen
            var originalSource = e.OriginalSource as FrameworkElement;
            while (originalSource != null && !(originalSource is Button))
            {
                originalSource = originalSource.Parent as FrameworkElement;
            }
            if (originalSource is Button button)
            {
                dragStartPoint = e.GetPosition(DockPanel);
                draggedButton = button;
                // Debug.WriteLine("DockPanel_MouseLeftButtonDown: Drag Start: " + draggedButton.Tag); // Debugging
            }
            else
            {
                // Debug.WriteLine("DockPanel_MouseLeftButtonDown: Kein Button als Quelle gefunden"); // Debugging
            }
        }


        private void DockPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            currentDockStatus |= DockStatus.MainDockHover; // Setzt das MainDockHover-Flag
            CheckAllConditions();
        }


        private void DockPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            currentDockStatus &= ~DockStatus.MainDockHover; // Löscht das MainDockHover-Flag
            // currentDockStatus &= ~DockStatus.DraggingToDock;
            CheckAllConditions();

            if (!DockContextMenu.IsOpen)
            {
                // ShowDock(); // Dock sichtbar halten
                currentDockStatus &= ~DockStatus.ContextMenuOpen;
                CheckAllConditions();
            }
        }




        public void ShowDock()
        {
            if (!dockVisible)
            {
                dockVisible = true;
                var duration = TimeSpan.FromMilliseconds(100);
                var slideAnimation = new ThicknessAnimation
                {
                    From = new Thickness(0, -DockPanel.ActualHeight + 5, 0, 0),
                    To = new Thickness(0, 0, 0, 0),
                    Duration = duration,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
                    FillBehavior = FillBehavior.HoldEnd
                };
                slideAnimation.Completed += (s, e) =>
                {
                    DockPanel.Margin = new Thickness(0, 0, 0, 0);
                    // Debug.WriteLine("Dock vollständig eingeblendet"); // Debugging
                };



                // Endkappen einblenden
                var endCapAnimation = new DoubleAnimation
                {
                    To = 0,
                    Duration = duration,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                if (LeftEndCap.RenderTransform == null)
                {
                    LeftEndCap.RenderTransform = new TranslateTransform();
                }
                if (RightEndCap.RenderTransform == null)
                {
                    RightEndCap.RenderTransform = new TranslateTransform();
                }
                DockPanel.BeginAnimation(FrameworkElement.MarginProperty, slideAnimation);
                LeftEndCap.RenderTransform.BeginAnimation(TranslateTransform.YProperty, endCapAnimation);
                RightEndCap.RenderTransform.BeginAnimation(TranslateTransform.YProperty, endCapAnimation);
            }
        }






        public void HideDock()
        {
            HideCategoryDockPanel();
            currentDockStatus = DockStatus.None;
            if (dockVisible)
            {
                dockVisible = false;
                var duration = TimeSpan.FromMilliseconds(100);
                var toValue = -DockPanel.ActualHeight + 5;
                var slideAnimation = new ThicknessAnimation
                {
                    From = new Thickness(0, 0, 0, 0),
                    To = new Thickness(0, -DockPanel.ActualHeight + toValue, 0, 0),
                    Duration = duration,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
                    FillBehavior = FillBehavior.HoldEnd
                };

                slideAnimation.Completed += (s, e) =>
                {
                    DockPanel.Margin = new Thickness(0, -DockPanel.ActualHeight + toValue, 0, 0);
                    // Debug.WriteLine("Dock teilweise ausgeblendet, 5 Pixel sichtbar"); // Debugging
                };



                // Endkappen ausblenden
                var endCapAnimation = new DoubleAnimation
                {
                    To = -DockPanel.ActualHeight + toValue,
                    Duration = duration,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                if (LeftEndCap.RenderTransform == null)
                {
                    LeftEndCap.RenderTransform = new TranslateTransform();
                }
                if (RightEndCap.RenderTransform == null)
                {
                    RightEndCap.RenderTransform = new TranslateTransform();
                }
                DockPanel.BeginAnimation(FrameworkElement.MarginProperty, slideAnimation);
                LeftEndCap.RenderTransform.BeginAnimation(TranslateTransform.YProperty, endCapAnimation);
                RightEndCap.RenderTransform.BeginAnimation(TranslateTransform.YProperty, endCapAnimation);
            }
            currentDockStatus = DockStatus.None;
            isCategoryDockOpen = false;

        }


        public void InitializeCategoryDockContainer(StackPanel container)
        {
            CategoryDockContainer = container;
        }




        private void CheckMousePosition(object sender, MouseEventArgs e)
        {
            if (DockPanel != null && e != null) // Sicherstellen, dass DockPanel und e nicht null sind
            {
                var mousePos = Mouse.GetPosition(DockPanel);
                var dockBounds = new Rect(DockPanel.TranslatePoint(new Point(), this), DockPanel.RenderSize); // Grenzen des Docks

                if (dockBounds.Contains(mousePos))  // Überprüfen, ob die Maus sich innerhalb der Grenzen des Docks befindet
                {
                    // Debug.WriteLine("Mouse over DockPanel");  // Debug-Ausgabe

                }
                else
                {
                    // Debug.WriteLine("Mouse not over DockPanel");  // Debug-Ausgabe
                    // HideDock();

                }
            }
            else
            {
                Debug.WriteLine("Fehler: DockPanel oder EventArgs sind null.");  // Debug-Ausgabe
            }
        }




        private void CategoryDockContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            currentDockStatus |= DockStatus.CategoryDockHover; // Setzt das CategoryDockHover-Flag
            // currentDockStatus &= ~DockStatus.CategoryElementClicked; // Löscht das CategoryElementClicked-Flag
            CheckAllConditions();
        }


        private void CategoryDockContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            currentDockStatus &= ~DockStatus.CategoryDockHover; // Löscht das CategoryDockHover-Flag
            CheckAllConditions();
        }

        private void DockPanel_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            currentDockStatus |= DockStatus.ContextMenuOpen; // Setzt das ContextMenuOpen-Flag
            OpenMenuItem.Visibility = Visibility.Collapsed;
            DeleteMenuItem.Visibility = Visibility.Collapsed;
            EditMenuItem.Visibility = Visibility.Collapsed;
            DockContextMenu.IsOpen = true;
            CheckAllConditions();
        }

        private void DockContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            currentDockStatus &= ~DockStatus.ContextMenuOpen; // Löscht das ContextMenuOpen-Flag
            CheckAllConditions();
        }

        public void CheckAllConditions()
        {
            // Debug.WriteLine($"Current Dock Status (numeric): {(int)currentDockStatus} - Flags: {currentDockStatus}"); // Debug-Ausgabe im Klartext

            if (currentDockStatus > 0) // Überprüft, ob irgendein Flag gesetzt ist
            {
                // Debug.WriteLine("Conditions met, showing dock."); // Debug-Ausgabe
                // ShowDock();
                categoryHideTimer.Stop();
                dockHideTimer.Stop();
            }
            else
            {
                // Debug.WriteLine("Conditions not met, hiding dock."); // Debug-Ausgabe
                categoryHideTimer.Start();
                dockHideTimer.Start();
            }
        }

        private Button? previousButton = null;
        public void CategoryDockContainer_MouseMove(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("CategoryDockContainer_MouseMove: gestartet"); // Debugging

            Point mousePosition = e.GetPosition(CategoryDockContainer);
            // LogMousePositionAndElements(mousePosition); // Optional, falls benötigt

            if (dragStartPoint.HasValue && draggedButton != null)
            {
                Point position = e.GetPosition(CategoryDockContainer);
                Vector diff = dragStartPoint.Value - position;

                if (e.LeftButton == MouseButtonState.Pressed &&
                    (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                     Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    // Debug.WriteLine($"Dragging: {draggedButton.Tag}, Position: {position}"); // Debugging
                    DragDrop.DoDragDrop(draggedButton, new DataObject(DataFormats.Serializable, draggedButton), DragDropEffects.Move);
                    dragStartPoint = null;
                    draggedButton = null;
                    isDragging = false; // Setze Dragging-Flag zurück
                }
            }
            else
            {
                bool isOverElement = false;
                UIElement? previousElement = null;
                UIElement? nextElement = null;

                for (int i = 0; i < CategoryDockContainer.Children.Count; i++)
                {
                    if (CategoryDockContainer.Children[i] is Button button)
                    {
                        Rect elementRect = new Rect(button.TranslatePoint(new Point(0, 0), CategoryDockContainer), button.RenderSize);
                        if (elementRect.Contains(mousePosition))
                        {
                            isOverElement = true;
                            if (!animationPlayed.ContainsKey(button) || !animationPlayed[button])
                            {
                                ButtonAnimations.AnimateButtonByChoice(button); // Methode aus der neuen Klasse aufrufen
                                animationPlayed[button] = true;
                            }
                            else if (button != previousButton)
                            {
                                ButtonAnimations.AnimateButtonByChoice(button); // Methode aus der neuen Klasse aufrufen
                            }
                            previousButton = button;
                            break;
                        }
                        else
                        {
                            previousElement = (i > 0) ? CategoryDockContainer.Children[i - 1] : null;
                            nextElement = CategoryDockContainer.Children[i];
                        }
                    }
                }

                if (!isOverElement)
                {
                    previousButton = null;
                    if (previousElement is Button prevButton && nextElement is Button nextButton)
                    {
                        // Debug.WriteLine($"Maus zwischen Elementen: {prevButton.Tag} und {nextButton.Tag}");
                    }
                    else if (nextElement is Button onlyNextButton)
                    {
                        // Debug.WriteLine($"Maus vor dem ersten Element: {onlyNextButton.Tag}");
                    }
                    else if (previousElement is Button onlyPrevButton)
                    {
                        // Debug.WriteLine($"Maus nach dem letzten Element: {onlyPrevButton.Tag}");
                    }
                    else
                    {
                        // Debug.WriteLine("Maus über CategoryDockContainer ohne Element");
                    }
                }
            }
        }


        public void CategoryDockContainer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Debug.WriteLine("CategoryDockContainer_PreviewMouseLeftButtonDown aufgerufen"); // Debugging

            if (sender is StackPanel categoryDock)
            {
                dragStartPoint = e.GetPosition(categoryDock);
                if (e.OriginalSource is FrameworkElement element && element.DataContext is DockItem)
                {
                    draggedButton = element as Button;
                    Debug.WriteLine($"Drag start point gesetzt: {dragStartPoint}, Element: {draggedButton?.Tag}"); // Debugging
                }
            }
        }

        private void DockPanel_MouseMove(object sender, MouseEventArgs e)
        {
            // Entferne gnadenlos alle Platzhalter, bevor der neue erstellt wird
            var allPlaceholders = DockPanel.Children.OfType<Border>().Where(border => border.Tag as string == "Placeholder").ToList();
            foreach (var placeholder in allPlaceholders)
            {
                DockPanel.Children.Remove(placeholder);
            }

            if (dragStartPoint.HasValue && draggedButton != null)
            {
                Point position = e.GetPosition(DockPanel);
                Vector diff = dragStartPoint.Value - position;

                if (e.LeftButton == MouseButtonState.Pressed &&
                    (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                     Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    if (!isDragging)
                    {
                        isDragging = true;
                        DragDrop.DoDragDrop(draggedButton, new DataObject(DataFormats.Serializable, draggedButton), DragDropEffects.Move);
                        dragStartPoint = null;
                        draggedButton = null;
                        isDragging = false; // Setze Dragging-Flag zurück
                    }
                }
            }
            else
            {
                Point mousePosition = e.GetPosition(DockPanel);
                bool isOverElement = false;

                for (int i = 0; i < DockPanel.Children.Count; i++)
                {
                    if (DockPanel.Children[i] is Button button)
                    {
                        Rect elementRect = new Rect(button.TranslatePoint(new Point(0, 0), DockPanel), button.RenderSize);

                        if (elementRect.Contains(mousePosition))
                        {
                            isOverElement = true;
                            break;
                        }
                    }
                }

                if (!isOverElement)
                {
                    // Debugging
                }
            }
        }




        private void DockPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = null;
            draggedButton = null;
            isDragging = false; // Dragging-Flag zurücksetzen
            Debug.WriteLine("Drag End"); // Debugging

            // Dock wieder ausblenden, falls die Maus außerhalb ist
            var mousePos = Mouse.GetPosition(this);
            var screenPos = PointToScreen(mousePos);
            if (screenPos.Y > this.Height + 10)
            {
                // HideDock();
            }
        }


        // private void DockPanel_DragEnter(object sender, DragEventArgs e)
        // {
        //     Debug.WriteLine("DockPanel_DragEnter: Aufgerufen"); // Debug-Ausgabe

        //     Point mousePosition = e.GetPosition(DockPanel);
        //     dockManager.LogMousePositionAndElements(mousePosition);

        //     CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
        //     currentDockStatus |= DockStatus.DraggingToDock;  // Flag setzen
        //     CheckAllConditions();

        //     // Position des Drag-Elements im DockPanel ermitteln
        //     Point dropPosition = e.GetPosition(DockPanel);
        //     Debug.WriteLine($"DockPanel_DragEnter: Drop-Position: X={dropPosition.X}, Y={dropPosition.Y}"); // Debug-Ausgabe der Position

        //     // Verzögerte Erstellung des Platzhalters
        //     Dispatcher.BeginInvoke(new Action(() =>
        //     {
        //         // Entferne gnadenlos alle Platzhalter, bevor der neue erstellt wird
        //         var allPlaceholders = DockPanel.Children.OfType<Border>().Where(border => border.Tag as string == "Placeholder").ToList();
        //         foreach (var placeholder in allPlaceholders)
        //         {
        //             DockPanel.Children.Remove(placeholder);
        //             Debug.WriteLine($"DockPanel_DragEnter: Entferne Platzhalter mit ID {placeholder.Uid}.");
        //         }

        //         // Platzhalter-Element (z.B. transparentes Border) mit eindeutiger ID
        //         currentPlaceholder = new Border
        //         {
        //             Background = new SolidColorBrush(Colors.LightGray),
        //             Opacity = 0.0,
        //             Height = 0.0, // Höhe des Platzhalters
        //             Width = 10, // Breite des Platzhalters
        //             Tag = "Placeholder",
        //             Uid = Guid.NewGuid().ToString() // Eindeutige ID hinzufügen
        //         };

        //         bool isMouseOnElement = false;

        //         // Durchlaufe die Kinder des DockPanels, um zu ermitteln, zwischen welchen Elementen das neue Element abgelegt wird
        //         for (int i = 0; i < DockPanel.Children.Count; i++)
        //         {
        //             // Überprüfen, ob das Kind ein Button ist
        //             if (DockPanel.Children[i] is Button button && button.Tag is DockItem dockItem)
        //             {
        //                 // Erhalte die Position jedes vorhandenen Elements
        //                 Point elementPosition = button.TransformToAncestor(DockPanel).Transform(new Point(0, 0));
        //                 double elementCenterX = elementPosition.X + (button.RenderSize.Width / 2);
        //                 Rect elementRect = new Rect(elementPosition, button.RenderSize);

        //                 Debug.WriteLine($"DockPanel_DragEnter: Element {i} - Position: X={elementPosition.X}, Y={elementPosition.Y}, CenterX={elementCenterX}"); // Debug-Ausgabe der Position des Elements

        //                 // Überprüfen, ob die Maus auf dem aktuellen Element ist
        //                 if (elementRect.Contains(dropPosition))
        //                 {
        //                     Debug.WriteLine($"DockPanel_DragEnter: Maus auf Element: DisplayName = {dockItem.DisplayName}, ID = {dockItem.Id}, Kategorie = {dockItem.Category}, IsCategory = {dockItem.IsCategory}");
        //                     isMouseOnElement = true;

        //                     if (dockItem.IsCategory)
        //                     {
        //                         Debug.WriteLine($"DockPanel_DragEnter: öffne KategorieDock = {dockItem.DisplayName}, ID = {dockItem.Id}");
        //                         // Kategoriedock öffnen
        //                         ShowCategoryDockPanel(new StackPanel
        //                         {
        //                             Tag = dockItem.Id
        //                         });
        //                         // Manuelles Auslösen von CategoryDockContainer_DragEnter
        //                         Application.Current.Dispatcher.Invoke(() =>
        //                         {
        //                             CategoryDockContainer_DragEnter(CategoryDockContainer, e);
        //                         });
        //                     }
        //                     else
        //                     {
        //                         HideCategoryDockPanel();
        //                         currentDockStatus &= ~DockStatus.CategoryElementClicked;
        //                     }

        //                     break;
        //                 }

        //                 // Überprüfen, ob die Drop-Position vor oder nach dem aktuellen Element ist
        //                 if (dropPosition.X < elementCenterX)
        //                 {
        //                     // Platzhalter immer hinzufügen, auch wenn der Index gleich ist
        //                     currentPlaceholderIndex = i; // Update den Platzhalter-Index
        //                     DockPanel.Children.Insert(i, currentPlaceholder);
        //                     Debug.WriteLine($"DockPanel_DragEnter: Platzhalter zwischen Element {i - 1} und Element {i} hinzugefügt.");
        //                     break;
        //                 }
        //             }
        //             else
        //             {
        //                 Debug.WriteLine($"DockPanel_DragEnter: Element {i} ist kein Button, sondern ein {DockPanel.Children[i].GetType().Name}.");
        //             }
        //         }

        //         if (!isMouseOnElement)
        //         {
        //             Debug.WriteLine("DockPanel_DragEnter: Maus zwischen Elementen oder außerhalb von Elementen.");
        //         }
        //     }));

        //     // Prüfen auf Serializable und FileDrop
        //     if ((e.Data.GetDataPresent(DataFormats.Serializable) || e.Data.GetDataPresent(DataFormats.FileDrop)) && DockPanel != null)
        //     {
        //         e.Effects = DragDropEffects.Move;
        //         // DockPanel.Background = new SolidColorBrush(Colors.Blue); // Visuelles Feedback
        //         DockPanel.Background = (SolidColorBrush)Application.Current.Resources["FeedbackColor"];

        //         Debug.WriteLine("DockPanel_DragEnter: Element über dem Hauptdock erkannt"); // Debug-Ausgabe
        //     }
        //     else
        //     {
        //         e.Effects = DragDropEffects.None;
        //         Debug.WriteLine("DockPanel_DragEnter: Kein Element erkannt oder DockPanel ist null"); // Debug-Ausgabe
        //     }
        // }

        private void RemoveDockPanelPlaceholders()
        {
            var allPlaceholders = DockPanel.Children.OfType<Border>().Where(border => border.Tag as string == "Placeholder").ToList();
            foreach (var placeholder in allPlaceholders)
            {
                DockPanel.Children.Remove(placeholder);
                Debug.WriteLine($"DockPanel_DragEnter: Entferne Platzhalter mit ID {placeholder.Uid}.");
            }
        }
        private void CreateDockPanelPlaceholder()
        {
            currentPlaceholder = new Border
            {
                Background = new SolidColorBrush(Colors.LightGray),
                Opacity = 1.0,
                Height = 60.0, // Höhe des Platzhalters
                Width = 3, // Breite des Platzhalters
                Tag = "Placeholder",
                Uid = Guid.NewGuid().ToString() // Eindeutige ID hinzufügen
            };
        }
        private bool IsMouseOnDockPanelElement(Point dropPosition, DragEventArgs e)
        {
            for (int i = 0; i < DockPanel.Children.Count; i++)
            {
                if (DockPanel.Children[i] is Button button && button.Tag is DockItem dockItem)
                {
                    Point elementPosition = button.TransformToAncestor(DockPanel).Transform(new Point(0, 0));
                    double elementCenterX = elementPosition.X + (button.RenderSize.Width / 2);
                    Rect elementRect = new Rect(elementPosition, button.RenderSize);

                    Debug.WriteLine($"DockPanel_DragEnter: Element {i} - Position: X={elementPosition.X}, Y={elementPosition.Y}, CenterX={elementCenterX}");

                    if (elementRect.Contains(dropPosition))
                    {
                        Debug.WriteLine($"DockPanel_DragEnter: Maus auf Element: DisplayName = {dockItem.DisplayName}, ID = {dockItem.Id}, Kategorie = {dockItem.Category}, IsCategory = {dockItem.IsCategory}");

                        if (dockItem.IsCategory)
                        {
                            Debug.WriteLine($"DockPanel_DragEnter: öffne KategorieDock = {dockItem.DisplayName}, ID = {dockItem.Id}");
                            ShowCategoryDockPanel(new StackPanel { Tag = dockItem.Id });
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                CategoryDockContainer_DragEnter(CategoryDockContainer, e);
                            });
                        }
                        else
                        {
                            HideCategoryDockPanel();
                            currentDockStatus &= ~DockStatus.CategoryElementClicked;
                        }

                        return true;
                    }

                    if (dropPosition.X < elementCenterX)
                    {
                        currentPlaceholderIndex = i;
                        DockPanel.Children.Insert(i, currentPlaceholder);
                        Debug.WriteLine($"DockPanel_DragEnter: Platzhalter zwischen Element {i - 1} und Element {i} hinzugefügt.");
                        break;
                    }
                }
            }

            return false;
        }

        private void DockPanel_DragEnter(object sender, DragEventArgs e)
        {
            Debug.WriteLine("DockPanel_DragEnter: Aufgerufen");

            Point mousePosition = e.GetPosition(DockPanel);
            dockManager.LogMousePositionAndElements(mousePosition);

            CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
            currentDockStatus |= DockStatus.DraggingToDock;
            CheckAllConditions();

            Point dropPosition = e.GetPosition(DockPanel);
            Debug.WriteLine($"DockPanel_DragEnter: Drop-Position: X={dropPosition.X}, Y={dropPosition.Y}");

            Dispatcher.BeginInvoke(new Action(() =>
            {
                RemoveDockPanelPlaceholders();
                CreateDockPanelPlaceholder();

                bool isMouseOnElement = IsMouseOnDockPanelElement(dropPosition, e);

                if (!isMouseOnElement)
                {
                    Debug.WriteLine("DockPanel_DragEnter: Maus zwischen Elementen oder außerhalb von Elementen.");
                }
            }));

            if ((e.Data.GetDataPresent(DataFormats.Serializable) || e.Data.GetDataPresent(DataFormats.FileDrop)) && DockPanel != null)
            {
                e.Effects = DragDropEffects.Move;
                DockPanel.Background = (SolidColorBrush)Application.Current.Resources["FeedbackColor"];
                Debug.WriteLine("DockPanel_DragEnter: Element über dem Hauptdock erkannt");
            }
            else
            {
                e.Effects = DragDropEffects.None;
                Debug.WriteLine("DockPanel_DragEnter: Kein Element erkannt oder DockPanel ist null");
            }
        }



        private void DockPanel_DragLeave(object sender, DragEventArgs e)
        {
            Debug.WriteLine("DockPanel_DragLeave: Aufgerufen"); // Debug-Ausgabe

            CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"]; // Visuelles Feedback zurücksetzen Farbe
            currentDockStatus &= ~DockStatus.DraggingToDock;  // Flag zurücksetzen, wenn der Drag-Vorgang das DockPanel verlässt
            CheckAllConditions();

            // Visuelles Feedback zurücksetzen
            var brush = (SolidColorBrush)FindResource("PrimaryColor");
            if (brush != null)
            {
                DockPanel.Background = brush; // Setze auf die ursprüngliche Farbe zurück
                Debug.WriteLine("DockPanel_DragLeave: Element hat das Hauptdock verlassen und Hintergrund zurückgesetzt"); // Debug-Ausgabe
            }
        }







        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Grid wurde angeklickt!");
        }





        private void Exit_Click(object sender, RoutedEventArgs e)
        {



            if (DockContextMenu.PlacementTarget is Button button && button.Tag is DockItem dockItem)
            {
                var customMessageBox = new CustomMessageBox($"Möchtest du BiMaDock wirklich beenden?");
                customMessageBox.ShowDialog();

                if (customMessageBox.Result)
                {
                    Application.Current.Shutdown();

                }
            }

        }

        public void UpdateDockItemLocation(Button button)
        {
            var dockItem = button.Tag as DockItem;
            if (dockItem != null)
            {
                // Aktuelle Kategorie ermitteln
                string currentCategory = dockItem.Category;

                // Aktualisiere die Position des Dock-Items
                dockManager.UpdateDockItemLocation(button, currentCategory); // currentCategory mitgeben
                Debug.WriteLine($"UpdateDockItemLocation: DockItem {dockItem.DisplayName} aktualisiert und gespeichert"); // Debug-Ausgabe
                RemoveCategoryDockPlaceholders();
            }
            else
            {
                Debug.WriteLine("UpdateDockItemLocation: DockItem ist null"); // Debug-Ausgabe
            }
        }

        public async void CategoryDockContainer_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Serializable) && !isCategoryMessageShown)
            {
                var button = e.Data.GetData(DataFormats.Serializable) as Button;
                if (button != null)
                {
                    var droppedItem = button.Tag as DockItem;
                    if (droppedItem != null && !string.IsNullOrEmpty(currentOpenCategory))
                    {
                        // Überprüfung auf Kategorie
                        if (droppedItem.IsCategory)
                        {
                            if (!isCategoryMessageShown)
                            {
                                isCategoryMessageShown = true; // Nachricht wurde gezeigt
                                HideDock();
                                HideCategoryDockPanel();
                            }
                            return; // Abbrechen, wenn es eine Kategorie ist
                        }

                        // Überprüfen, ob das Element bereits einer anderen Kategorie zugewiesen ist
                        if (string.IsNullOrEmpty(droppedItem.Category) || droppedItem.Category == currentOpenCategory)
                        {
                            // Entferne das Element nicht, wenn es vom Hauptdock kommt und keine Kategorie hat
                            if (!string.IsNullOrEmpty(droppedItem.Category))
                            {
                                var parent = VisualTreeHelper.GetParent(button) as Panel;
                                if (parent != null)
                                {
                                    parent.Children.Remove(button);
                                }
                            }

                            droppedItem.Category = currentOpenCategory;

                            // Logische Trennung durchführen, bevor das Element hinzugefügt wird
                            if (button.Parent != null)
                            {
                                var logicalParent = LogicalTreeHelper.GetParent(button) as Panel;
                                if (logicalParent != null)
                                {
                                    logicalParent.Children.Remove(button);
                                }
                            }

                            // Position innerhalb des Kategorie-Docks bestimmen
                            Point dropPosition = e.GetPosition(CategoryDockContainer);
                            double dropCenterX = dropPosition.X;
                            int newIndex = 0;
                            bool inserted = false;
                            for (int i = 0; i < CategoryDockContainer.Children.Count; i++)
                            {
                                if (CategoryDockContainer.Children[i] is Button existingButton)
                                {
                                    Point elementPosition = existingButton.TranslatePoint(new Point(0, 0), CategoryDockContainer);
                                    double elementCenterX = elementPosition.X + (existingButton.ActualWidth / 2);

                                    if (dropCenterX < elementCenterX)
                                    {
                                        CategoryDockContainer.Children.Insert(i, button);
                                        inserted = true;
                                        break;
                                    }
                                }
                                newIndex++;
                            }
                            if (!inserted)
                            {
                                CategoryDockContainer.Children.Add(button);
                            }

                            // Visuelles Feedback zurücksetzen Farbe
                            CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];

                            // Aktualisiere die interne Struktur oder Daten, falls nötig
                            UpdateDockItemLocation(button);
                            // Dock-Items speichern
                            dockManager.SaveDockItems(currentOpenCategory); // Verwende die gespeicherte Kategorie
                                                                            // ** Erzwinge visuelles Update **
                            button.Visibility = Visibility.Collapsed;
                            button.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.Text))
            {
                string? rawData = e.Data.GetData(DataFormats.Text) as string;
                if (rawData != null)
                {
                    string url = rawData.ToString();
                    var dockItem = new DockItem
                    {
                        FilePath = url,
                        DisplayName = url,
                        Category = currentOpenCategory
                    };

                    Point dropPosition = e.GetPosition(CategoryDockContainer);
                    double dropCenterX = dropPosition.X;
                    int newIndex = 0;
                    bool inserted = false;
                    for (int i = 0; i < CategoryDockContainer.Children.Count; i++)
                    {
                        if (CategoryDockContainer.Children[i] is Button existingButton)
                        {
                            Point elementPosition = existingButton.TranslatePoint(new Point(0, 0), CategoryDockContainer);
                            double elementCenterX = elementPosition.X + (existingButton.ActualWidth / 2);

                            if (dropCenterX < elementCenterX)
                            {
                                CategoryDockContainer.Children.Insert(i, new Button { Content = dockItem.DisplayName, Tag = dockItem });
                                inserted = true;
                                break;
                            }
                        }
                        newIndex++;
                    }
                    if (!inserted)
                    {
                        CategoryDockContainer.Children.Add(new Button { Content = dockItem.DisplayName, Tag = dockItem });
                    }

                    // Visuelles Feedback zurücksetzen Farbe
                    CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];

                    // Aktualisiere die interne Struktur oder Daten, falls nötig
                    UpdateDockItemLocation(new Button { Content = dockItem.DisplayName, Tag = dockItem });

                    // Dock-Items speichern
                    dockManager.SaveDockItems(currentOpenCategory); // Verwende die gespeicherte Kategorie
                    HideDock();
                    ShowCategoryDockPanel(new StackPanel
                    {
                        Tag = currentOpenCategory
                    });
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    // Überprüfen, ob es sich um eine Verknüpfung handelt und den Zielpfad ermitteln
                    string targetPath = file;
                    if (System.IO.Path.GetExtension(file).ToLower() == ".lnk" || System.IO.Path.GetExtension(file).ToLower() == ".url")
                    {
                        targetPath = await GetShortcutTarget.GetShortcutTargetAsync(file);
                    }

                    var dockItem = new DockItem
                    {
                        FilePath = targetPath ?? file,
                        DisplayName = System.IO.Path.GetFileNameWithoutExtension(file) ?? string.Empty,
                        Category = currentOpenCategory
                    };

                    Point dropPosition = e.GetPosition(CategoryDockContainer);
                    double dropCenterX = dropPosition.X;
                    int newIndex = 0;
                    bool inserted = false;
                    for (int i = 0; i < CategoryDockContainer.Children.Count; i++)
                    {
                        if (CategoryDockContainer.Children[i] is Button existingButton)
                        {
                            Point elementPosition = existingButton.TranslatePoint(new Point(0, 0), CategoryDockContainer);
                            double elementCenterX = elementPosition.X + (existingButton.ActualWidth / 2);

                            if (dropCenterX < elementCenterX)
                            {
                                CategoryDockContainer.Children.Insert(i, new Button { Content = dockItem.DisplayName, Tag = dockItem });
                                inserted = true;
                                break;
                            }
                        }
                        newIndex++;
                    }
                    if (!inserted)
                    {
                        CategoryDockContainer.Children.Add(new Button { Content = dockItem.DisplayName, Tag = dockItem });
                    }

                    // Visuelles Feedback zurücksetzen Farbe
                    CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];

                    // Aktualisiere die interne Struktur oder Daten, falls nötig
                    UpdateDockItemLocation(new Button { Content = dockItem.DisplayName, Tag = dockItem });

                    // Dock-Items speichern
                    dockManager.SaveDockItems(currentOpenCategory); // Verwende die gespeicherte Kategorie
                    HideDock();
                    ShowCategoryDockPanel(new StackPanel
                    {
                        Tag = currentOpenCategory
                    });
                }
            }
            else
            {
                e.Handled = true;
            }
            // Visuelles Feedback zurücksetzen
            CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
            CheckAllConditions();
        }


        private void CreateCategoryDockPlaceholder(Point dropPosition)
        {
            var placeholder = new Border
            {
                Background = new SolidColorBrush(Colors.LightGray),
                Opacity = 1.0,
                Height = 60.0, // Höhe des Platzhalters
                Width = 1, // Breite des Platzhalters
                Tag = "CategoryPlaceholder",
                Uid = Guid.NewGuid().ToString() // Eindeutige ID hinzufügen
            };

            // Finde die richtige Position für den Platzhalter im CategoryDockContainer
            for (int i = 0; i < CategoryDockContainer.Children.Count; i++)
            {
                if (CategoryDockContainer.Children[i] is Button button)
                {
                    Point elementPosition = button.TransformToAncestor(CategoryDockContainer).Transform(new Point(0, 0));
                    double elementCenterX = elementPosition.X + (button.RenderSize.Width / 2);

                    if (dropPosition.X < elementCenterX)
                    {
                        CategoryDockContainer.Children.Insert(i, placeholder);
                        Debug.WriteLine($"CategoryDockContainer_DragEnter: Platzhalter zwischen Element {i - 1} und Element {i} hinzugefügt.");
                        return;
                    }
                }
            }

            // Wenn keine passende Position gefunden wurde, füge den Platzhalter am Ende hinzu
            CategoryDockContainer.Children.Add(placeholder);
            Debug.WriteLine("CategoryDockContainer_DragEnter: Platzhalter am Ende hinzugefügt.");
        }



        private void RemoveCategoryDockPlaceholders()
        {
            var allPlaceholders = CategoryDockContainer.Children.OfType<Border>().Where(border => border.Tag as string == "CategoryPlaceholder").ToList();
            foreach (var placeholder in allPlaceholders)
            {
                CategoryDockContainer.Children.Remove(placeholder);
                Console.WriteLine($"CategoryDockContainer_DragEnter: Entferne Platzhalter mit ID {placeholder.Uid}.");
            }
        }

        private bool HasCategoryMouseMovedSignificantly(Point currentMousePosition)
        {
            if (previousCategoryMousePosition == null)
            {
                previousCategoryMousePosition = currentMousePosition;
                return true;
            }

            double distance = (currentMousePosition - previousCategoryMousePosition.Value).Length;
            const double movementThreshold = 10; // Schwellenwert in Pixeln

            if (distance > movementThreshold)
            {
                previousCategoryMousePosition = currentMousePosition;
                return true;
            }

            return false;
        }


        public void CategoryDockContainer_DragEnter(object sender, DragEventArgs e)
        {
            Console.WriteLine("CategoryDockContainer_DragEnter aufgerufen");

            DebugLogDataFormats(e);

            Point dropPosition = e.GetPosition(CategoryDockContainer); // Ermitteln der Drop-Position

            RemoveCategoryDockPlaceholders();
            CreateCategoryDockPlaceholder(dropPosition); // Platzhalter an der richtigen Position erstellen

            if (IsRelevantDataFormatPresent(e))
            {
                ProcessRelevantDataFormats(e);
            }
            else
            {
                ProcessNoRelevantDataFormat(e);
            }

            CheckAllConditions();
        }


        private void DebugLogDataFormats(DragEventArgs e)
        {
            Console.WriteLine("Vorhandene Datenformate:");
            var formats = e.Data.GetFormats();
            foreach (var format in formats)
            {
                Console.WriteLine($" - {format}");
            }
        }
        private bool IsRelevantDataFormatPresent(DragEventArgs e)
        {
            return e.Data.GetDataPresent("Shell IDList Array") || e.Data.GetDataPresent("FileDrop") ||
                   e.Data.GetDataPresent("FileNameW") || e.Data.GetDataPresent("FileName") ||
                   e.Data.GetDataPresent("FileGroupDescriptorW") || e.Data.GetDataPresent("FileContents") ||
                   e.Data.GetDataPresent(DataFormats.Serializable);
        }

        private void ProcessRelevantDataFormats(DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["FeedbackColor"];
            Console.WriteLine("CategoryDockContainer_DragEnter: Element über dem Kategoriedock erkannt");

            var categoryName = CategoryDockContainer.Tag as string;
            Console.WriteLine($"CategoryDockContainer_DragEnter: Geöffnete categoryName: {categoryName}");

            if (!string.IsNullOrEmpty(categoryName))
            {
                currentOpenCategory = categoryName;
                Console.WriteLine($"CategoryDockContainer_DragEnter: Geöffnete Kategorie: {currentOpenCategory}");
            }
        }

        private void ProcessNoRelevantDataFormat(DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            Console.WriteLine("CategoryDockContainer_DragEnter: Kein passendes Datenformat erkannt");
            CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
        }

        public void CategoryDockContainer_DragLeave(object sender, DragEventArgs e)
        {
            Debug.WriteLine("CategoryDockContainer_DragLeave aufgerufen"); // Debug-Ausgabe

            // Kategorie-Status nur zurücksetzen, wenn keine anderen Elemente gerade gezogen werden
            if (draggedButton == null)
            {
                currentOpenCategory = "";
                Debug.WriteLine("Geöffnete Kategorie zurückgesetzt"); // Debug-Ausgabe
            }
            CategoryDockBorder.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
            // CategoryDockContainer.Background = new SolidColorBrush(Colors.Transparent); // Visuelles Feedback zurücksetzen Farbe
            Debug.WriteLine("Element hat das Kategoriedock verlassen"); // Debug-Ausgabe
        }








        public void OpenDockItem(DockItem dockItem)
        {
            Debug.WriteLine($"OpenDockItem aufgerufen"); // Debug-Ausgabe
            if (!string.IsNullOrEmpty(dockItem.FilePath))
            {
                Debug.WriteLine($"OpenDockItem aufgerufen, filePath: {dockItem.FilePath}"); // Debug-Ausgabe
                OpenFile(dockItem.FilePath);
            }
            else
            {
                Debug.WriteLine("OpenDockItem aufgerufen, Kategorie"); // Debug-Ausgabe
                // if (isCategoryDockOpen && currentOpenCategory == dockItem.DisplayName)
                if (isCategoryDockOpen && currentOpenCategory == dockItem.Id)
                {
                    // Kategoriedock schließen
                    CategoryDockContainer.Visibility = Visibility.Collapsed;
                    CategoryDockBorder.Visibility = Visibility.Collapsed;
                    OverlayCanvas.Visibility = Visibility.Collapsed;
                    currentDockStatus &= ~DockStatus.CategoryElementClicked; // Flag zurücksetzen

                    isCategoryDockOpen = false;
                    Debug.WriteLine($"OpenDockItem Kategoriedock {dockItem.DisplayName} ID: {dockItem.Id} geschlossen"); // Debug-Ausgabe
                    isCategoryDockOpenID = "";
                }
                else
                {
                    // Beispielaufruf der Methode
                    // string validName = GetValidName(dockItem.Id);

                    // Kategoriedock öffnen
                    ShowCategoryDockPanel(new StackPanel
                    {
                        Tag = dockItem.Id
                        // Children = { new Button { Content = $"Kategorie: {dockItem.DisplayName}", Width = 100, Height = 50 } }
                    });










                    currentDockStatus |= DockStatus.CategoryElementClicked; // Flag setzen
                    isCategoryDockOpen = true;
                    Debug.WriteLine($"OpenDockItem Kategoriedock {dockItem.DisplayName} ID: {dockItem.Id} geöffnet"); // Debug-Ausgabe
                    isCategoryDockOpenID = dockItem.Id;
                }
            }
            CheckAllConditions();
        }

        // Hilfsmethode zum Generieren eines gültigen Namens
        private string GetValidName(string guid)
        {
            return "_" + guid.Replace("-", "");
        }






        public void OpenFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                    Debug.WriteLine($"Dateipfad: {filePath}"); // Debug-Ausgabe
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler beim Öffnen der Datei: {ex.Message}"); // Debug-Ausgabe
                }
            }
            else
            {
                Debug.WriteLine("Fehler: Kein Dateipfad bereitgestellt"); // Debug-Ausgabe
            }
            Debug.WriteLine("OpenFile: Dock schließen");
            HideCategoryDockPanel();
            HideDock();
            currentDockStatus = DockStatus.None;
            // CheckAllConditions();


        }



        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (DockContextMenu.PlacementTarget is Button button && button.Tag is DockItem dockItem)
            {
                var customMessageBox = new CustomMessageBox($"Möchtest du das Element '{dockItem.DisplayName}' wirklich löschen?");
                customMessageBox.ShowDialog();

                if (customMessageBox.Result)
                {
                    // Hier ermitteln wir die Kategorie, die gelöscht werden soll
                    string currentCategory = dockItem.Category;

                    // Übergabe der Kategorie an RemoveDockItem
                    dockManager.RemoveDockItem(button, currentCategory);

                    HideCategoryDockPanel();

                }
            }
        }










        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            var inputDialog = new InputDialog("Kategorie erstellen", "Bitte geben Sie den Namen der Kategorie ein:");
            if (inputDialog.ShowDialog() == true)
            {
                string categoryName = inputDialog.Answer;
                dockManager.AddCategoryItem(categoryName);
            }
        }

        public void ShowCategoryDockPanel(StackPanel categoryDock)
        {
            // Debug.WriteLine("ShowCategoryDockPanel: Methode aufgerufen");
            CategoryDockContainer.Children.Clear();
            CategoryDockContainer.Children.Add(categoryDock);
            CategoryDockContainer.Visibility = Visibility.Visible;
            CategoryDockBorder.Visibility = Visibility.Visible;
            OverlayCanvas.Visibility = Visibility.Visible;
            Panel.SetZIndex(OverlayCanvas, 1000);
            Panel.SetZIndex(CategoryDockBorder, 0);

            currentDockStatus |= DockStatus.CategoryElementClicked;
            currentOpenCategory = categoryDock.Tag?.ToString() ?? string.Empty;
            CategoryDockContainer.Tag = currentOpenCategory;
            // Debug.WriteLine($"ShowCategoryDockPanel: currentOpenCategory = {currentOpenCategory}");

            var items = SettingsManager.LoadSettings();
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.Category) && item.Category == currentOpenCategory)
                {
                    dockManager.AddDockItemAt(item, CategoryDockContainer.Children.Count, currentOpenCategory);
                    // Debug.WriteLine($"ShowCategoryDockPanel: DockItem {item.DisplayName} zur Kategorie {currentOpenCategory} hinzugefügt");
                }
            }

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Debug.WriteLine("ShowCategoryDockPanel: Dispatcher aufgerufen");

                if (CategoryDockBorder.ActualWidth > 0)
                {
                    double mainWindowCenterX = Application.Current.MainWindow.ActualWidth / 2;
                    double mainStackPanelCenterX = MainStackPanel.ActualWidth / 2;
                    double elementCenterX = dockManager.mousePositionSave + mainStackPanelCenterX;
                    double categoryDockPositionX = dockManager.mousePositionSave + dockManager.mousePositionSave;
                    // Setze die neue Position
                    CategoryDockBorder.Margin = new Thickness(categoryDockPositionX, 0, 0, 0);

                    double categoryDockCenterX = categoryDockPositionX + (CategoryDockBorder.ActualWidth / 2);
                    double positionRelativeToCenter = categoryDockCenterX - mainWindowCenterX;

                    double overlayPositionX = dockManager.mousePositionSaveleft - 10;
                    double overlayPositionY = 80;

                    Canvas.SetLeft(OverlayCanvasHorizontalLine, overlayPositionX);
                    Canvas.SetTop(OverlayCanvasHorizontalLine, overlayPositionY);
                    Panel.SetZIndex(OverlayCanvasHorizontalLine, 1000); // Höherer Wert bringt es in den Vordergrund

                }
                else
                {
                    // Debug.WriteLine("ShowCategoryDockPanel: CategoryDockBorder.ActualWidth ist 0 oder kleiner");
                }
            }, System.Windows.Threading.DispatcherPriority.Loaded);

            // Einblendanimation hinzufügen
            DoubleAnimation fadeInAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.5) // Dauer der Animation
            };

            // Animation starten, wenn das Element sichtbar wird
            CategoryDockContainer.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);

            MainStackPanel.Margin = new Thickness(0);
            categoryHideTimer.Start();
            CheckAllConditions();
        }


        public void HideCategoryDockPanel()
        {
            CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];// Visuelles Feedback zurücksetzen Farbe
            // Debug.WriteLine("HideCategoryDockPanel aufgerufen"); // Debugging
            CategoryDockContainer.Visibility = Visibility.Collapsed;
            CategoryDockBorder.Visibility = Visibility.Collapsed; // Sichtbarkeit der CategoryDockBorder ändern
            OverlayCanvas.Visibility = Visibility.Collapsed;

            // MainStackPanel zurücksetzen
            MainStackPanel.Margin = new Thickness(0, 0, 0, 0);

            // Timer stoppen
            categoryHideTimer.Stop();
            // Debug.WriteLine("CategoryDockContainer ausgeblendet, MainStackPanel neu positioniert."); // Debugging
            // CheckAllConditions();
            isCategoryMessageShown = false; // Nachricht-Flag sofort zurücksetzen
            isCategoryDockOpen = false;
        }


        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            currentDockStatus |= DockStatus.EditSettingsDock;  // Flag setzen
            try
            {
                if (DockContextMenu.PlacementTarget is Button button && button.Tag is DockItem dockItem)
                {
                    Debug.WriteLine($"Edit_Click Geklicktes Item: ID = {dockItem.Id}, Name = {dockItem.DisplayName} Category= {dockItem.Category}");

                    if (dockItem.IsCategory)
                    {
                        HideCategoryDockPanel();
                    }

                    // Alle Dock-Items laden
                    var dockItems = SettingsManager.LoadSettings() ?? new List<DockItem>();

                    // Prüfen, ob das aktuelle Item existiert
                    var settings = dockItems.FirstOrDefault(di => di.Id == dockItem.Id);

                    if (settings == null)
                    {
                        MessageBox.Show("Fehler beim Laden der Dock-Einstellungen.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }


                    // Edit-Dialog initialisieren
                    EditPropertiesWindow editWindow = new EditPropertiesWindow
                    {
                        Owner = this,
                        IdTextBox = { Text = settings.Id },
                        NameTextBox = { Text = settings.DisplayName },
                        IconSourceTextBox = { Text = settings.IconSource }, // Hinzufügen des Bildpfads
                        CategoryTextBox = { Text = settings.Category },
                        IsCategoryTextBox = { Text = settings.IsCategory.ToString() },
                        ApplicationPathTextBox = { Text = settings.FilePath.ToString() },

                        DockItem = settings
                    };
                    Debug.WriteLine($"Edit_Click: Übergebenes DockItem: ID = {settings.Id}, Name = {settings.DisplayName}, IconSource = {settings.IconSource}, Kategorie = {settings.Category}, Ist Kategorie = {settings.IsCategory}");

                    bool? dialogResult = editWindow.ShowDialog();
                    if (dialogResult == true)
                    {
                        string newName = editWindow.NameTextBox.Text.Trim();
                        string newIconPath = editWindow.IconSourceTextBox.Text.Trim(); // Neues Bildpfad

                        // Neuen Namen validieren
                        if (string.IsNullOrEmpty(newName))
                        {
                            MessageBox.Show("Name darf nicht leer sein.", "Ungültiger Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Prüfen, ob der Name geändert wurde
                        bool nameChanged = settings.DisplayName != newName;

                        if (nameChanged)
                        {
                            string oldCategory = settings.DisplayName;
                            settings.DisplayName = newName;

                            // Aktualisiere alle untergeordneten Elemente, die zur alten Kategorie gehören, falls das Element eine Kategorie ist
                            foreach (var item in dockItems)
                            {
                                if (item.Category == oldCategory)
                                {
                                    item.Category = newName;
                                }
                            }

                            // Aktualisiere den Button-Text
                            var textBlock = new TextBlock
                            {
                                Text = newName,
                                TextAlignment = TextAlignment.Center,
                                TextWrapping = TextWrapping.Wrap,
                                Width = 60,
                                Margin = new Thickness(5)
                            };

                            var stackPanel = button.Content as StackPanel;
                            if (stackPanel != null)
                            {
                                stackPanel.Children.RemoveAt(1);
                                stackPanel.Children.Add(textBlock);
                            }
                        }

                        // Wenn der Name nicht geändert wurde oder das Symbol aktualisiert werden muss
                        if (!string.IsNullOrEmpty(newIconPath))
                        {
                            settings.IconSource = newIconPath; // Hier den Bildpfad aktualisieren
                        }

                        button.Tag = dockItem;

                        // Speichern der aktualisierten Einstellungen
                        SettingsManager.SaveSettings(dockItems);
                        // dockManager.LoadDockItems(); dockItem.Category

                        if (!string.IsNullOrEmpty(dockItem.Category))
                        {
                            HideCategoryDockPanel();
                            StackPanel categoryDock = new StackPanel
                            {
                                Tag = dockItem.Category
                            };
                            ShowCategoryDockPanel(categoryDock);
                        }
                        else
                        {
                            dockManager.LoadDockItems();
                        }


                    }
                }
                else
                {
                    MessageBox.Show("Fehler: DockContextMenu.PlacementTarget ist kein Button oder button.Tag ist kein DockItem", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void CheckAutostart()
        {
            AutostartCheckBox.IsChecked = StartupManager.IsInStartup();
        }

        private void AutostartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (AutostartCheckBox.IsChecked == true)
            {
                AutostartCheckBox.IsChecked = false;
                StartupManager.AddToStartup(false);
            }
            else
            {
                AutostartCheckBox.IsChecked = true;
                StartupManager.AddToStartup(true);
            }
        }



        private void AutostartCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            StartupManager.AddToStartup(true);
        }

        private void AutostartCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            StartupManager.AddToStartup(false);
        }


        protected override void OnClosed(EventArgs e)
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged; // Event abmelden, um Speicherlecks zu vermeiden
            base.OnClosed(e);
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        private async void UpdateCheck()
        {
            await UpdateChecker.CheckForUpdatesAsync();

        }


        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://bandit-1200.github.io/BiMaDock";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der URL: {ex.Message}");
            }
        }



        private void Test_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OpenFilePath_Click(object sender, RoutedEventArgs e)
        {
            // Debug.WriteLine($"Edit_Click: Übergebenes DockItem: ID = {settings.Id}, Name = {settings.DisplayName}, IconSource = {settings.IconSource}, Kategorie = {settings.Category}, Ist Kategorie = {settings.IsCategory}");
            Debug.WriteLine($"OpenFilePath_Click: ");
            if (DockContextMenu.PlacementTarget is Button button && button.Tag is DockItem dockItem)
            {
                Debug.WriteLine($"OpenFilePath_Click: Geklicktes Item: ID = {dockItem.Id}, Name = {dockItem.DisplayName} Category= {dockItem.Category}, Pfad= {dockItem.FilePath}");
                string filePath = dockItem.FilePath;
                if (!string.IsNullOrEmpty(filePath))
                {
                    // Startet den Explorer und zeigt die Datei an 
                    Process.Start("explorer.exe", $"/select,{filePath}");
                }
            }
        }


        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CenterWindow();
        }

        private void CenterWindow()
        {
            // Bildschirmbreite abrufen
            double screenWidth = SystemParameters.PrimaryScreenWidth;

            // Fensterbreite abrufen
            double windowWidth = this.Width;

            // Neue horizontale Position berechnen (zentriert)
            this.Left = (screenWidth - windowWidth) / 2;

            // Vertikale Position fixieren (am oberen Bildschirmrand)
            this.Top = 0;
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            Debug.WriteLine("Die Bildschirmauflösung hat sich geändert."); // Debug-Ausgabe
            CenterWindow(); // Fenster neu zentrieren
        }
    }

}