using System.Runtime.InteropServices;

namespace BiMaDock.Native;

/// <summary>
/// Meldet, wenn ein anderes Fenster in den Vordergrund kommt oder das Vordergrundfenster Größe oder Lage ändert
/// (z. B. RDP wechselt ins Vollbild). Windows ruft dafür direkt zurück, es wird nichts abgefragt.
/// Muss auf einem Thread mit Nachrichtenschleife erzeugt werden (UI-Thread).
/// </summary>
internal sealed partial class ForegroundWatcher : IDisposable
{
    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;
    private const int OBJID_WINDOW = 0;

    private readonly Action _changed;
    private readonly WinEventProc _callback; // Feld hält den Delegaten am Leben, solange Windows ihn aufruft
    private readonly IntPtr _foregroundHook;
    private readonly IntPtr _locationHook;

    public ForegroundWatcher(Action changed)
    {
        _changed = changed;
        _callback = OnEvent;
        _foregroundHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _callback, 0, 0,
            WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
        _locationHook = SetWinEventHook(EVENT_OBJECT_LOCATIONCHANGE, EVENT_OBJECT_LOCATIONCHANGE, IntPtr.Zero, _callback, 0, 0,
            WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
    }

    /// <summary>Beide Benachrichtigungen sind aktiv; sonst braucht es eine Ersatzabfrage.</summary>
    public bool IsActive => _foregroundHook != IntPtr.Zero && _locationHook != IntPtr.Zero;

    private void OnEvent(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint thread, uint time)
    {
        // Lageänderungen kommen sehr oft (auch vom Mauszeiger): nur das Vordergrundfenster selbst zählt.
        if (eventType == EVENT_OBJECT_LOCATIONCHANGE && (idObject != OBJID_WINDOW || idChild != 0 || hwnd != GetForegroundWindow()))
        {
            return;
        }

        _changed();
    }

    public void Dispose()
    {
        if (_foregroundHook != IntPtr.Zero)
        {
            UnhookWinEvent(_foregroundHook);
        }

        if (_locationHook != IntPtr.Zero)
        {
            UnhookWinEvent(_locationHook);
        }
    }

    private delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc,
        uint idProcess, uint idThread, uint dwFlags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnhookWinEvent(IntPtr hWinEventHook);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetForegroundWindow();
}
