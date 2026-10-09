using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BiMaDock
{
    public partial class MainWindow
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
    }
}
