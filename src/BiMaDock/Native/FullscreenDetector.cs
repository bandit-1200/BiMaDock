using System.Runtime.InteropServices;

namespace BiMaDock.Native;

/// <summary>Erkennt, ob das Vordergrundfenster einen ganzen Bildschirm füllt (z. B. RDP-Vollbild, Spiele, Videos).</summary>
internal static partial class FullscreenDetector
{
    private const uint MONITOR_DEFAULTTONULL = 0;

    /// <summary>True, wenn ein fremdes Vordergrundfenster den Monitor von <paramref name="ownWindow"/> komplett bedeckt.</summary>
    public static bool IsForegroundFullscreen(IntPtr ownWindow, out IntPtr foreground, out string className)
    {
        className = string.Empty;
        foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero || foreground == ownWindow
            || foreground == GetShellWindow() || foreground == GetDesktopWindow())
        {
            return false;
        }

        // Desktop-Hintergrund (Progman/WorkerW) zählt nicht als Vollbild.
        var buffer = new char[32];
        int length = GetClassName(foreground, buffer, buffer.Length);
        className = new string(buffer, 0, Math.Max(length, 0));
        if (className is "Progman" or "WorkerW")
        {
            return false;
        }

        IntPtr monitor = MonitorFromWindow(ownWindow, MONITOR_DEFAULTTONULL);
        if (monitor == IntPtr.Zero || MonitorFromWindow(foreground, MONITOR_DEFAULTTONULL) != monitor)
        {
            return false;
        }

        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info) || !GetWindowRect(foreground, out var rect))
        {
            return false;
        }

        return rect.Left <= info.Monitor.Left && rect.Top <= info.Monitor.Top
            && rect.Right >= info.Monitor.Right && rect.Bottom >= info.Monitor.Bottom;
    }

    /// <summary>Legt <paramref name="hwnd"/> in der Z-Reihenfolge direkt hinter <paramref name="above"/>, ohne es zu aktivieren.</summary>
    public static void PlaceBehind(IntPtr hwnd, IntPtr above) =>
        SetWindowPos(hwnd, above, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
    }

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetShellWindow();

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetDesktopWindow();

    [LibraryImport("user32.dll", EntryPoint = "GetClassNameW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int GetClassName(IntPtr hWnd, [Out] char[] className, int maxCount);

    [LibraryImport("user32.dll")]
    private static partial IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(IntPtr hWnd, out NativeRect rect);
}
