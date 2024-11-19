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
        // Überprüfen, ob der nCode größer oder gleich 0 ist und es sich um einen Mausklick handelt
        if (nCode >= 0 && (MouseMessages)wParam == MouseMessages.WM_LBUTTONDOWN)
        {
            // Mausklick-Ereignis erkannt

            // Marshal.PtrToStructure könnte null zurückgeben, daher Null-Überprüfung
            var hookStruct = Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

            // Sicherstellen, dass die Struktur nicht null ist
            if (hookStruct != null)
            {
                MSLLHOOKSTRUCT msllHookStruct = (MSLLHOOKSTRUCT)hookStruct;
                Point mousePosition = new Point(msllHookStruct.pt.x, msllHookStruct.pt.y);

                // Überprüfen, ob der Klick innerhalb des Anwendungsfensters ist
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var window = mainWindow;
                    Rect windowRect = new Rect(window.Left, window.Top, window.Width, window.Height);

                    if (!windowRect.Contains(mousePosition))
                    {
                        // Ereignis auslösen, da der Klick außerhalb des Fensters ist
                        window.HideDock();
                        // Debug.WriteLine("Klick außerhalb der Anwendung!");
                    }
                    else
                    {
                        // Debug.WriteLine("Klick innerhalb der Anwendung!");

                        // Hit-Test, um das angeklickte Element zu bestimmen
                        Point relativePoint = window.PointFromScreen(mousePosition);
                        HitTestResult result = VisualTreeHelper.HitTest(window, relativePoint);

                        if (result != null)
                        {
                            var element = result.VisualHit as FrameworkElement;
                            if (element != null)
                            {
                                // Überprüfe die Sichtbarkeit des Elements
                                if (element.Visibility == Visibility.Visible)
                                {
                                    // Debug.WriteLine($"Element unter der Maus: {element.Name}, Typ: {element.GetType().Name}, Sichtbar");
                                    if (element.GetType().Name == "Border")
                                    {
                                        // Debug.WriteLine($"Element unter der Maus ist ein Border:");
                                        mainWindow.HideCategoryDockPanel();
                                        mainWindow.HideDock();
                                    }
                                }
                                else
                                {
                                    // Debug.WriteLine($"Element unter der Maus: {element.Name}, Typ: {element.GetType().Name}, Nicht sichtbar");
                                }
                            }
                        }
                    }
                });
            }
            else
            {
                // Fehlerbehandlung, falls hookStruct null ist
                // Debug.WriteLine("Fehler: Die Struktur konnte nicht erstellt werden (lParam ungültig).");
            }
        }

        // Ruf den nächsten Hook auf, um sicherzustellen, dass das Hook-System weiter funktioniert
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }


    private void OnClickOutside()
    {
        // Hier kannst du die Logik einfügen, die ausgeführt werden soll, wenn der Klick außerhalb der Anwendung stattfindet
        // Debug.WriteLine("Ereignis: Klick außerhalb der Anwendung erkannt!");
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

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private enum MouseMessages
    {
        WM_LBUTTONDOWN = 0x0201,
        // Weitere Mausnachrichten falls nötig
    }
}
