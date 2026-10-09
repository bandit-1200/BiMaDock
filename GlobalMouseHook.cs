using System;
using System.Runtime.InteropServices;
using System.Windows;  // Für Point und Rect
using System.Windows.Media;  // Für HitTestResult
using BiMaDock;  // Importiere den richtigen Namespace

public class GlobalMouseHook
{
    private readonly MainWindow mainWindow;
    private IntPtr _hookID = IntPtr.Zero;
    private readonly LowLevelMouseProc _proc;
    private bool _isHookInstalled;

    // Der Hook wird nur installiert, solange das Dock sichtbar ist (siehe MainWindow.ShowDock/HideDock),
    // damit nicht dauerhaft jeder systemweite Mausklick durch BiMaDock läuft.
    public GlobalMouseHook(MainWindow window)
    {
        mainWindow = window;
        _proc = HookCallback;
    }

    public void SetHook()
    {
        if (_isHookInstalled || mainWindow == null)
        {
            return;
        }

        _hookID = SetWindowsHookEx(WH_MOUSE_LL, _proc, IntPtr.Zero, 0);
        _isHookInstalled = _hookID != IntPtr.Zero;
    }

    public void Unhook()
    {
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
            _isHookInstalled = false;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0 || (MouseMessages)wParam != MouseMessages.WM_LBUTTONDOWN)
        {
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        if (mainWindow == null || !mainWindow.IsLoaded)
        {
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        MSLLHOOKSTRUCT msllHookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
        Point mousePosition = new Point(msllHookStruct.pt.x, msllHookStruct.pt.y);

        // Asynchron auswerten, damit der Hook sofort zurückkehrt und die Maus systemweit nicht bremst.
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var window = mainWindow;
            var editPropertiesWindow = GetOpenEditPropertiesWindow();
            bool isEditPropertiesWindowOpen = editPropertiesWindow != null && editPropertiesWindow.IsVisible;

            if (!isEditPropertiesWindowOpen)
            {
                Point relativePoint = window.PointFromScreen(mousePosition);
                HitTestResult result = VisualTreeHelper.HitTest(window, relativePoint);

                if (result != null)
                {
                    var element = result.VisualHit as FrameworkElement;
                    if (element != null)
                    {
                        if (!IsElementChildOf(element, window.MainGrid) && !IsElementChildOf(element, window.CategoryDockBorder))
                        {
                            window.HideDock();
                            window.HideCategoryDockPanel();
                            window.currentDockStatus = MainWindow.DockStatus.None;
                        }
                    }
                }
                else
                {
                    window.HideDock();
                    window.HideCategoryDockPanel();
                }
            }
        });

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

    private enum MouseMessages
    {
        WM_LBUTTONDOWN = 0x0201,
        // Weitere Mausnachrichten falls nötig
    }
}
