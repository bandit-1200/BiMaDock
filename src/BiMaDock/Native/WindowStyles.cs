using System.Runtime.InteropServices;

namespace BiMaDock.Native;

internal static partial class WindowStyles
{
    private const int GWL_EXSTYLE = -20;
    private const long WS_EX_TRANSPARENT = 0x00000020;
    private const long WS_EX_TOOLWINDOW = 0x00000080;
    private const long WS_EX_LAYERED = 0x00080000;
    private const long WS_EX_NOACTIVATE = 0x08000000;
    private const long WS_EX_APPWINDOW = 0x00040000;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    /// <summary>Blendet das Fenster aus Alt+Tab und der Taskleiste aus.</summary>
    public static void MakeToolWindow(IntPtr hwnd) => UpdateExStyle(hwnd, WS_EX_TOOLWINDOW, WS_EX_APPWINDOW);

    /// <summary>Fenster ignoriert die Maus vollständig und wird nie aktiviert.</summary>
    public static void MakeClickThrough(IntPtr hwnd) =>
        UpdateExStyle(hwnd, WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE, WS_EX_APPWINDOW);

    public static bool TryGetCursorPos(out int x, out int y)
    {
        bool ok = GetCursorPos(out var point);
        x = point.X;
        y = point.Y;
        return ok;
    }

    public static void MoveWithoutActivating(IntPtr hwnd, int x, int y) =>
        SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);

    private static void UpdateExStyle(IntPtr hwnd, long add, long remove)
    {
        long style = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
        style = (style | add) & ~remove;
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(style));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static partial IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static partial IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out NativePoint point);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
}
