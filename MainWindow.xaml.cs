using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32; // Für SystemEvents

namespace BiMaDock
{
    public partial class MainWindow : Window
    {

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private DispatcherTimer timer;

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

        const int SW_MINIMIZE = 6;

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

        private Border? currentPlaceholder = null; // Referenz auf den Platzhalter

        public void SetDragging(bool value)
        {
            isDragging = value;
        }

        public Point? ActiveDragStartPoint
        {
            get => activeDragStartPoint;
            set => activeDragStartPoint = value;
        }

        public Button? ActiveDraggedButton
        {
            get => activeDraggedButton;
            set => activeDraggedButton = value;
        }

        private Point? activeDragStartPoint;
        private Button? activeDraggedButton;

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

        private DispatcherTimer? showDockTimer; // neu: Verzögerung beim Einblenden (nullable)

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

                    DockPanel.DragEnter += (s, e) =>
                    {
                        e.Effects = DragDropEffects.All;
                        if (!dockVisible) ShowDock();
                    };
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
        /// <summary>
        /// Ermittelt, ob ein Remotedesktop-Fenster (mstsc) im Vordergrund ist.
        /// Prüft nur die Fensterklasse statt die Prozessliste zu durchsuchen; ein minimiertes
        /// oder im Hintergrund liegendes RDP-Fenster nimmt dem Dock so nicht mehr den Vordergrund.
        /// </summary>
        private static bool IsRdpSessionActive(IntPtr foregroundWindow)
        {
            var className = new StringBuilder(256);
            return GetClassName(foregroundWindow, className, className.Capacity) > 0
                && className.ToString() == "TscShellContainerClass";
        }

        // Weitere Initialisierung
        private void CheckForRdpFullScreen(object? sender, EventArgs e)
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            bool rdpSessionActive = IsRdpSessionActive(foregroundWindow);
            RECT rect;

            if (GetWindowRect(foregroundWindow, out rect))
            {
                // Überprüfen, ob das Fenster minimiert ist
                if (IsWindowMinimized(foregroundWindow))
                {
                    if (!this.Topmost)
                    {
                        this.Topmost = true;
                    }
                }

                if (rdpSessionActive)
                {
                    if (this.Topmost)
                    {
                        this.Topmost = false;
                    }
                }
                else
                {
                    if (!this.Topmost)
                    {
                        this.Topmost = true;
                    }
                }
            }
            else
            {
                if (!this.Topmost)
                {
                    this.Topmost = true;
                }
            }
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

        private void CategoryDockContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            currentDockStatus |= DockStatus.CategoryDockHover; // Setzt das CategoryDockHover-Flag
            CheckAllConditions();
        }


        private void CategoryDockContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            currentDockStatus &= ~DockStatus.CategoryDockHover; // Löscht das CategoryDockHover-Flag
            CheckAllConditions();
        }

        private void MainWindow_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = GetDropEffect(e.Data);
            if (e.Effects != DragDropEffects.None)
            {
                ShowDock();
            }
        }

        private void MainWindow_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = GetDropEffect(e.Data);
            if (e.Effects != DragDropEffects.None)
            {
                ShowDock();
            }
        }

        private void MainDockBorder_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = GetDropEffect(e.Data);
            if (e.Effects != DragDropEffects.None)
            {
                ShowDock();
            }
        }

        private void MainDockBorder_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = GetDropEffect(e.Data);
            if (e.Effects != DragDropEffects.None)
            {
                ShowDock();
            }
        }

        internal static DragDropEffects GetDropEffect(IDataObject data)
        {
            if (data.GetDataPresent(DockDropPosition.DockItemButtonFormat))
            {
                return DragDropEffects.Move;
            }

            return DockDropPosition.HasFilePathData(data)
                || data.GetDataPresent(DataFormats.UnicodeText)
                || data.GetDataPresent(DataFormats.Text)
                    ? DragDropEffects.Copy
                    : DragDropEffects.None;
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

        private Button? previousButton = null;
        public void CategoryDockContainer_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                return;
            }

            Point mousePosition = e.GetPosition(CategoryDockContainer);
            bool isOverElement = false;

            for (int i = 0; i < CategoryDockContainer.Children.Count; i++)
            {
                if (CategoryDockContainer.Children[i] is Button button)
                {
                    Rect elementRect = new Rect(button.TranslatePoint(new Point(0, 0), CategoryDockContainer), button.RenderSize);
                    if (elementRect.Contains(mousePosition))
                    {
                        isOverElement = true;
                        if (!animationPlayed.ContainsKey(button) || !animationPlayed[button] || button != previousButton)
                        {
                            ButtonAnimations.AnimateButtonByChoice(button);
                        }
                        animationPlayed[button] = true;
                        previousButton = button;
                        break;
                    }
                }
            }
            if (!isOverElement)
            {
                previousButton = null;
            }
        }

        private void RemoveDockPanelPlaceholders()
        {
            var allPlaceholders = DockPanel.Children.OfType<Border>().Where(border => border.Tag as string == "Placeholder").ToList();
            foreach (var placeholder in allPlaceholders)
            {
                DockPanel.Children.Remove(placeholder);
            }
        }
        private void CreateDockPanelPlaceholder()
        {
            currentPlaceholder ??= new Border
            {
                Background = new SolidColorBrush(Colors.LightGray),
                Opacity = 1.0,
                Height = 60.0, // Höhe des Platzhalters
                Width = 3, // Breite des Platzhalters
                Tag = "Placeholder",
                Uid = Guid.NewGuid().ToString() // Eindeutige ID hinzufügen
            };
        }

        private void UpdateDockPanelPlaceholder(double dropX)
        {
            RemoveDockPanelPlaceholders();
            CreateDockPanelPlaceholder();
            int insertionIndex = DockDropPosition.FindInsertionIndex(DockPanel, dropX);
            DockPanel.Children.Insert(insertionIndex, currentPlaceholder);
        }

        private bool IsMouseOnDockPanelElement(Point dropPosition, DragEventArgs e)
        {
            for (int i = 0; i < DockPanel.Children.Count; i++)
            {
                if (DockPanel.Children[i] is Button button && button.Tag is DockItem dockItem)
                {
                    Point elementPosition = button.TransformToAncestor(DockPanel).Transform(new Point(0, 0));
                    Rect elementRect = new Rect(elementPosition, button.RenderSize);


                    if (elementRect.Contains(dropPosition))
                    {

                        if (dockItem.IsCategory)
                        {
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
                }
            }

            return false;
        }

        private void DockPanel_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = GetDropEffect(e.Data);
            if (e.Effects == DragDropEffects.None)
            {
                return;
            }

            ShowDock();
            dockManager.LogMousePositionAndElements(e.GetPosition(DockPanel));

            CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
            currentDockStatus |= DockStatus.DraggingToDock;
            CheckAllConditions();

            Point dropPosition = e.GetPosition(DockPanel);
            IsMouseOnDockPanelElement(dropPosition, e);
            UpdateDockPanelPlaceholder(dropPosition.X);
            DockPanel.Background = (SolidColorBrush)Application.Current.Resources["FeedbackColor"];
        }

        private void DockPanel_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = GetDropEffect(e.Data);
            if (e.Effects == DragDropEffects.None)
            {
                RemoveDockPanelPlaceholders();
                return;
            }

            Point dropPosition = e.GetPosition(DockPanel);
            UpdateDockPanelPlaceholder(dropPosition.X);
        }



        private void DockPanel_DragLeave(object sender, DragEventArgs e)
        {
            RemoveDockPanelPlaceholders();

            CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"]; // Visuelles Feedback zurücksetzen Farbe
            currentDockStatus &= ~DockStatus.DraggingToDock;  // Flag zurücksetzen, wenn der Drag-Vorgang das DockPanel verlässt
            CheckAllConditions();

            // Visuelles Feedback zurücksetzen
            var brush = (SolidColorBrush)FindResource("PrimaryColor");
            if (brush != null)
            {
                DockPanel.Background = brush; // Setze auf die ursprüngliche Farbe zurück
            }
        }











        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var customMessageBox = new CustomMessageBox("Möchtest du BiMaDock wirklich beenden?")
                {
                    Owner = this
                };

                customMessageBox.ShowDialog();

                if (customMessageBox.Result)
                {
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exit_Click Fehler: {ex}");
                MessageBox.Show(this, "Beim Beenden von BiMaDock ist ein Fehler aufgetreten.", "Beenden", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public async void CategoryDockContainer_Drop(object sender, DragEventArgs e)
        {
            RemoveCategoryDockPlaceholders();
            Point dropPosition = e.GetPosition(CategoryDockContainer);

            try
            {
                if (string.IsNullOrEmpty(currentOpenCategory))
                {
                    return;
                }

                if (e.Data.GetDataPresent(DockDropPosition.DockItemButtonFormat))
                {
                    if (e.Data.GetData(DockDropPosition.DockItemButtonFormat) is Button button &&
                        button.Tag is DockItem droppedItem &&
                        !droppedItem.IsCategory)
                    {
                        (VisualTreeHelper.GetParent(button) as Panel)?.Children.Remove(button);
                        droppedItem.Category = currentOpenCategory;
                        int insertionIndex = DockDropPosition.FindInsertionIndex(CategoryDockContainer, dropPosition.X);
                        CategoryDockContainer.Children.Insert(insertionIndex, button);
                        dockManager.SaveDockItems(currentOpenCategory);
                    }
                }
                else if (DockDropPosition.GetDroppedFilePaths(e.Data) is string[] files)
                {
                    await AddFilesToCategoryAsync(files, dropPosition.X);
                    HideDock();
                    ShowCategoryDockPanel(new StackPanel { Tag = currentOpenCategory });
                }
                else if (TryGetDroppedText(e.Data, out string text))
                {
                    int insertionIndex = DockDropPosition.FindInsertionIndex(CategoryDockContainer, dropPosition.X);
                    var dockItem = new DockItem
                    {
                        FilePath = text,
                        DisplayName = text,
                        Category = currentOpenCategory
                    };
                    dockManager.AddDockItemAt(dockItem, insertionIndex, currentOpenCategory, saveChanges: false);
                    dockManager.SaveDockItems(currentOpenCategory);
                    HideDock();
                    ShowCategoryDockPanel(new StackPanel { Tag = currentOpenCategory });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CategoryDockContainer_Drop: Fehler aufgetreten - {ex}");
                MessageBox.Show(this, "Das Element konnte nicht zur Kategorie hinzugefügt werden.",
                    "BiMaDock", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                e.Handled = true;
                CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
                CheckAllConditions();
            }
        }

        private Task AddFilesToCategoryAsync(string[] files, double dropX)
        {
            var dockItemsToAdd = new List<DockItem>(files.Length);
            foreach (string file in files)
            {
                dockItemsToAdd.Add(new DockItem
                {
                    FilePath = file,
                    DisplayName = System.IO.Path.GetFileNameWithoutExtension(file),
                    Category = currentOpenCategory
                });
            }

            int insertionIndex = DockDropPosition.FindInsertionIndex(CategoryDockContainer, dropX);
            foreach (var dockItem in dockItemsToAdd)
            {
                dockManager.AddDockItemAt(dockItem, insertionIndex++, currentOpenCategory, saveChanges: false);
            }

            dockManager.SaveDockItems(currentOpenCategory);
            return Task.CompletedTask;
        }

        private static bool TryGetDroppedText(IDataObject data, out string text)
        {
            string? format = data.GetDataPresent(DataFormats.UnicodeText)
                ? DataFormats.UnicodeText
                : data.GetDataPresent(DataFormats.Text)
                    ? DataFormats.Text
                    : null;
            text = format is not null ? data.GetData(format) as string ?? string.Empty : string.Empty;
            return !string.IsNullOrWhiteSpace(text);
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
                        return;
                    }
                }
            }

            // Wenn keine passende Position gefunden wurde, füge den Platzhalter am Ende hinzu
            CategoryDockContainer.Children.Add(placeholder);
        }



        private void RemoveCategoryDockPlaceholders()
        {
            var allPlaceholders = CategoryDockContainer.Children.OfType<Border>().Where(border => border.Tag as string == "CategoryPlaceholder").ToList();
            foreach (var placeholder in allPlaceholders)
            {
                CategoryDockContainer.Children.Remove(placeholder);
            }
        }

        public void CategoryDockContainer_DragEnter(object sender, DragEventArgs e)
        {
            if (CategoryDockContainer.Tag is string categoryName)
            {
                currentOpenCategory = categoryName;
            }

            CategoryDockContainer_DragOver(sender, e);
        }

        public void CategoryDockContainer_DragOver(object sender, DragEventArgs e)
        {
            if (!dockVisible)
            {
                ShowDock();
            }

            e.Effects = GetDropEffect(e.Data);
            if (e.Effects == DragDropEffects.Move &&
                e.Data.GetData(DockDropPosition.DockItemButtonFormat) is Button button &&
                button.Tag is DockItem droppedItem &&
                droppedItem.IsCategory)
            {
                e.Effects = DragDropEffects.None;
            }

            RemoveCategoryDockPlaceholders();
            if (e.Effects != DragDropEffects.None)
            {
                CreateCategoryDockPlaceholder(e.GetPosition(CategoryDockContainer));
                CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["FeedbackColor"];
            }
            else
            {
                CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
            }
        }

        public void CategoryDockContainer_DragLeave(object sender, DragEventArgs e)
        {
            RemoveCategoryDockPlaceholders();

            if (!isDragging)
            {
                currentOpenCategory = "";
            }
            CategoryDockBorder.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
            CategoryDockContainer.Background = (SolidColorBrush)Application.Current.Resources["PrimaryColor"];
        }






        public void OpenDockItem(DockItem dockItem)
        {
            if (!string.IsNullOrEmpty(dockItem.FilePath))
            {
                OpenFile(dockItem.FilePath);
            }
            else
            {
                if (isCategoryDockOpen && currentOpenCategory == dockItem.Id)
                {
                    // Kategoriedock schließen
                    CategoryDockContainer.Visibility = Visibility.Collapsed;
                    CategoryDockBorder.Visibility = Visibility.Collapsed;
                    OverlayCanvas.Visibility = Visibility.Collapsed;
                    currentDockStatus &= ~DockStatus.CategoryElementClicked; // Flag zurücksetzen

                    isCategoryDockOpen = false;
                    isCategoryDockOpenID = "";
                }
                else
                {
                    // Kategoriedock öffnen
                    ShowCategoryDockPanel(new StackPanel
                    {
                        Tag = dockItem.Id
                    });







                    currentDockStatus |= DockStatus.CategoryElementClicked; // Flag setzen
                    isCategoryDockOpen = true;
                    isCategoryDockOpenID = dockItem.Id;
                }
            }
            CheckAllConditions();
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
            HideCategoryDockPanel();
            HideDock();
            currentDockStatus = DockStatus.None;


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
            try
            {
                var inputDialog = new InputDialog("Kategorie erstellen", "Bitte geben Sie den Namen der Kategorie ein:")
                {
                    Owner = this
                };

                if (inputDialog.ShowDialog() == true)
                {
                    string categoryName = inputDialog.Answer?.Trim() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(categoryName))
                    {
                        MessageBox.Show(this, "Bitte geben Sie einen gültigen Kategorienamen ein.", "Kategorie erstellen", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var existingItems = SettingsManager.LoadSettings();
                    bool alreadyExists = existingItems.Any(item =>
                        item.IsCategory &&
                        string.Equals(item.DisplayName, categoryName, StringComparison.OrdinalIgnoreCase));

                    if (alreadyExists)
                    {
                        MessageBox.Show(this, $"Die Kategorie \"{categoryName}\" existiert bereits.", "Kategorie erstellen", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    dockManager.AddCategoryItem(categoryName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AddCategory_Click Fehler: {ex}");
                MessageBox.Show(this, $"Beim Erstellen der Kategorie ist ein Fehler aufgetreten:\n{ex.Message}", "Kategorie erstellen", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowCategoryDockPanel(StackPanel categoryDock)
        {
            CategoryDockContainer.Children.Clear();
            CategoryDockContainer.Visibility = Visibility.Visible;
            CategoryDockBorder.Visibility = Visibility.Visible;
            OverlayCanvas.Visibility = Visibility.Visible;
            Panel.SetZIndex(OverlayCanvas, 1000);
            Panel.SetZIndex(CategoryDockBorder, 0);

            currentDockStatus |= DockStatus.CategoryElementClicked;
            currentOpenCategory = categoryDock.Tag?.ToString() ?? string.Empty;
            CategoryDockContainer.Tag = currentOpenCategory;

            var items = SettingsManager.LoadSettings();
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.Category) && item.Category == currentOpenCategory)
                {
                    dockManager.AddDockItemAt(item, CategoryDockContainer.Children.Count, currentOpenCategory, saveChanges: false);
                }
            }

            CategoryDockScrollViewer.ScrollToHorizontalOffset(0);
            categoryDockNavigation.Refresh();

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
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
                PositionCategoryDock(currentOpenCategory);
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
            CategoryDockContainer.Visibility = Visibility.Collapsed;
            CategoryDockBorder.Visibility = Visibility.Collapsed; // Sichtbarkeit der CategoryDockBorder ändern
            OverlayCanvas.Visibility = Visibility.Collapsed;

            // MainStackPanel zurücksetzen
            MainStackPanel.Margin = new Thickness(0, 0, 0, 0);

            // Timer stoppen
            categoryHideTimer.Stop();
            isCategoryDockOpen = false;
        }

        private void CategoryDockArea_MouseEnter(object sender, MouseEventArgs e)
        {
            currentDockStatus |= DockStatus.CategoryDockHover;
            dockHideTimer?.Stop();
            categoryHideTimer?.Stop();
        }

        private void CategoryDockArea_MouseLeave(object sender, MouseEventArgs e)
        {
            currentDockStatus &= ~DockStatus.CategoryDockHover;
            CheckAllConditions();
        }

        private void PositionCategoryDock(string categoryId)
        {
            CategoryDockPositioner.Position(
                categoryId,
                DockPanel,
                MainStackPanel,
                MainGrid,
                CategoryDockBorder,
                OverlayCanvasHorizontalLine);
        }

        private void MainDockPreviousButton_Click(object sender, RoutedEventArgs e) =>
            mainDockNavigation.ScrollPrevious();

        private void MainDockNextButton_Click(object sender, RoutedEventArgs e) =>
            mainDockNavigation.ScrollNext();

        private void CategoryDockPreviousButton_Click(object sender, RoutedEventArgs e) =>
            categoryDockNavigation.ScrollPrevious();

        private void CategoryDockNextButton_Click(object sender, RoutedEventArgs e) =>
            categoryDockNavigation.ScrollNext();

        private void MainDockScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            mainDockNavigation.ScrollByWheelDelta(e.Delta);
        }

        private void CategoryDockScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            categoryDockNavigation.ScrollByWheelDelta(e.Delta);
        }

        private void DockScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (ReferenceEquals(sender, MainDockScrollViewer))
            {
                mainDockNavigation.Refresh();
            }
            else if (ReferenceEquals(sender, CategoryDockScrollViewer))
            {
                categoryDockNavigation.Refresh();
            }
        }

        private void RefreshDockNavigation()
        {
            mainDockNavigation.Refresh();
            categoryDockNavigation.Refresh();
        }


        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            currentDockStatus |= DockStatus.EditSettingsDock;  // Flag setzen
            try
            {
                if (DockContextMenu.PlacementTarget is Button button && button.Tag is DockItem dockItem)
                {

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
                            settings.DisplayName = newName;

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

                        // Speichern der aktualisierten Einstellungen
                        SettingsManager.SaveSettings(dockItems);

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

        // Das Umschalten löst AutostartCheckBox_Checked/_Unchecked aus, die den Registry-Eintrag schreiben.
        private void AutostartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AutostartCheckBox.IsChecked = AutostartCheckBox.IsChecked != true;
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
            timer?.Stop();
            mouseHook?.Unhook();
            mouseHook = null;
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged; // Event abmelden, um Speicherlecks zu vermeiden
            base.OnClosed(e);
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AboutWindow aboutWindow = new AboutWindow
                {
                    Owner = this
                };
                aboutWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AboutMenuItem_Click Fehler: {ex}");
                MessageBox.Show(this, $"Beim Öffnen des Info-Fensters ist ein Fehler aufgetreten:\n{ex.Message}", "Über BiMaDock", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UpdateCheck()
        {
            if (Environment.GetEnvironmentVariable("BIMADOCK_DISABLE_UPDATE_CHECK") == "1")
            {
                return;
            }

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

        private void OpenFilePath_Click(object sender, RoutedEventArgs e)
        {
            if (DockContextMenu.PlacementTarget is Button button && button.Tag is DockItem dockItem)
            {
                string filePath = dockItem.FilePath;
                if (!string.IsNullOrEmpty(filePath))
                {
                    // Startet den Explorer und zeigt die Datei an 
                    var startInfo = new ProcessStartInfo("explorer.exe");
                    startInfo.ArgumentList.Add($"/select,{filePath}");
                    Process.Start(startInfo);
                }
            }
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
