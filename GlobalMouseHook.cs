using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;  // Für Point und Rect
using System.Windows.Media;  // Für HitTestResult
using System.Windows.Interop;
using BiMaDock;  // Importiere den richtigen Namespace

public class GlobalMouseHook
{
    private MainWindow mainWindow;
    private IntPtr _hookID = IntPtr.Zero;
    private LowLevelMouseProc _proc;

    public GlobalMouseHook(MainWindow window)
    {
        mainWindow = window;
        _proc = HookCallback; // HookCallback delegieren
        SetHook();
    }

    public void SetHook()
    {
        _hookID = SetWindowsHookEx(WH_MOUSE_LL, _proc, IntPtr.Zero, 0);
    }

    public void Unhook()
    {
        UnhookWindowsHookEx(_hookID);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (MouseMessages)wParam == MouseMessages.WM_LBUTTONDOWN)
        {
            var hookStruct = Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

            if (hookStruct != null)
            {
                MSLLHOOKSTRUCT msllHookStruct = (MSLLHOOKSTRUCT)hookStruct;
                Point mousePosition = new Point(msllHookStruct.pt.x, msllHookStruct.pt.y);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var window = mainWindow;
                    var editPropertiesWindow = GetOpenEditPropertiesWindow();

                    // Prüfen, ob das EditPropertiesWindow noch geöffnet ist
                    bool isEditPropertiesWindowOpen = editPropertiesWindow != null && editPropertiesWindow.IsVisible;

                    if (!isEditPropertiesWindowOpen)
                    {
                        Rect mainWindowRect = new Rect(window.Left, window.Top, window.Width, window.Height);
                        Point relativePoint = window.PointFromScreen(mousePosition);
                        HitTestResult result = VisualTreeHelper.HitTest(window, relativePoint);

                        if (result != null)
                        {
                            var element = result.VisualHit as FrameworkElement;
                            if (element != null)
                            {
                                // Überprüfen, ob das Element innerhalb des Hauptdocks oder Kategoriedocks ist
                                if (!IsElementChildOf(element, window.MainGrid) && !IsElementChildOf(element, window.CategoryDockBorder))
                                {
                                    // Klick außerhalb des ersten Grids und des CategoryDockPanels erkannt
                                    window.HideDock();
                                    window.HideCategoryDockPanel();
                                    window.currentDockStatus = MainWindow.DockStatus.None;
                                    Console.WriteLine("Klick außerhalb des ersten Grids und des CategoryDockPanels erkannt!");
                                }
                            }
                        }
                        else
                        {
                            // Mausklick außerhalb der Anwendung erkannt
                            window.HideDock();
                            window.HideCategoryDockPanel(); // Schließt das Kategoriedock
                            Console.WriteLine("Klick außerhalb der Anwendung erkannt!");
                        }
                    }
                });
            }
            else
            {
                Debug.WriteLine("Fehler: Die Struktur konnte nicht erstellt werden (lParam ungültig).");
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private bool IsElementChildOf(FrameworkElement element, FrameworkElement parent)
    {
        while (element != null)
        {
            if (element == parent)
            {
                return true;
            }

            var parentElement = VisualTreeHelper.GetParent(element) as FrameworkElement;
            if (parentElement == null)
            {
                return false;
            }
            element = parentElement;
        }
        return false;
    }


    private EditPropertiesWindow? GetOpenEditPropertiesWindow()
    {
        foreach (Window window in Application.Current.Windows)
        {
            if (window is EditPropertiesWindow)
            {
                return (EditPropertiesWindow)window;
            }
        }
        return null;
    }







    // private bool IsElementChildOf(DependencyObject element, DependencyObject parent)
    // {
    //     while (element != null)
    //     {
    //         if (element == parent)
    //             return true;

    //         element = VisualTreeHelper.GetParent(element);
    //     }
    //     return false;
    // }


    // private void OnClickOutsideGrid()
    // {
    //     Debug.WriteLine("Klick außerhalb des ersten Grids erkannt!");
    //     mainWindow.HideDock();
    // }

    // Struktur für Mausinformationen
    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public int mouseData;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    // Definiere den Delegaten
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    private const int WH_MOUSE_LL = 14;

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private enum MouseMessages
    {
        WM_LBUTTONDOWN = 0x0201,
        // Weitere Mausnachrichten falls nötig
    }
}
