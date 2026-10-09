using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using Microsoft.Win32;

namespace BiMaDock.UITests;

/// <summary>
/// Startet BiMaDock mit eigenem, temporärem Datenordner und bietet Hilfsfunktionen zur Bedienung.
/// Echte Benutzerdaten, Update-Prüfung und der echte Autostart-Eintrag bleiben unberührt.
/// </summary>
public sealed class BiMaDockSession : IDisposable
{
    public const string StartupValueName = "BiMaDock.UITest";
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // Kürzeste erlaubte Einblendverzögerung (Standard 300 ms), damit ShowDock schneller greift.
    private const string FastStyleSettingsJson = "{\"DockShowDelayMilliseconds\":100}";

    private readonly Application app;

    static BiMaDockSession()
    {
        // FlaUI animiert Mouse.MoveTo standardmäßig mit 0,5 px/ms (mehrere Sekunden über breite Bildschirme).
        // Die Hilfsfunktionen setzen die Position direkt; dies gilt nur als Rückfall für übrige MoveTo-Aufrufe.
        Mouse.MovePixelsPerMillisecond = 20;
        Mouse.MovePixelsPerStep = 50;
    }

    public UIA3Automation Automation { get; } = new();
    public Window MainWindow { get; }
    public string DataDirectory { get; }
    public int ProcessId => app.ProcessId;
    public bool HasExited => app.HasExited;

    public BiMaDockSession(params TestDockItem[] seedItems)
    {
        DataDirectory = Path.Combine(Path.GetTempPath(), "BiMaDock.UITests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DataDirectory);
        if (seedItems.Length > 0)
        {
            File.WriteAllText(DockSettingsPath, JsonSerializer.Serialize(seedItems));
        }
        File.WriteAllText(StyleSettingsPath, FastStyleSettingsJson);

        RemoveStartupValue();

        string exePath = Path.Combine(AppContext.BaseDirectory, "BiMaDock.exe");
        var startInfo = new ProcessStartInfo(exePath) { UseShellExecute = false, WorkingDirectory = AppContext.BaseDirectory };
        startInfo.Environment["BIMADOCK_DATA_DIR"] = DataDirectory;
        startInfo.Environment["BIMADOCK_DISABLE_UPDATE_CHECK"] = "1";
        startInfo.Environment["BIMADOCK_STARTUP_VALUE_NAME"] = StartupValueName;

        app = Application.Launch(startInfo);
        try
        {
            // ShowInTaskbar=False macht das Dock zu einem besessenen Fenster ohne Process.MainWindowHandle,
            // daher über die Top-Level-Fenster des Prozesses suchen.
            Window? mainWindow = null;
            WaitUntil(() => (mainWindow = app.GetAllTopLevelWindows(Automation).FirstOrDefault(w => w.Title == "BiMaDock")) != null,
                "BiMaDock-Hauptfenster wurde nicht gefunden.");
            MainWindow = mainWindow!;
            WaitUntil(() => FindAll(ControlType.Button).Length > 0 || seedItems.Length == 0, "Dock-Elemente wurden nicht geladen.");
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public string DockSettingsPath => Path.Combine(DataDirectory, "docksettings.json");
    public string StyleSettingsPath => Path.Combine(DataDirectory, "StyleSettings.json");

    public List<TestDockItem> ReadDockItems()
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return JsonSerializer.Deserialize<List<TestDockItem>>(File.ReadAllText(DockSettingsPath), JsonOptions) ?? new();
            }
            catch (IOException) when (attempt < 20)
            {
                Thread.Sleep(50); // Datei wird gerade atomar ersetzt.
            }
        }
    }

    public AutomationElement[] FindAll(ControlType type) =>
        MainWindow.FindAllDescendants(cf => cf.ByControlType(type));

    public AutomationElement? FindDockButton(string name) =>
        MainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button).And(cf.ByName(name)));

    public AutomationElement GetDockButton(string name)
    {
        AutomationElement? button = null;
        WaitUntil(() => (button = FindDockButton(name)) != null, $"Dock-Element '{name}' nicht gefunden.");
        return button!;
    }

    /// <summary>Das Dock gilt als sichtbar, wenn die Elemente nicht über den oberen Bildschirmrand hinausragen.</summary>
    public bool IsDockVisible(string anyItemName)
    {
        var button = FindDockButton(anyItemName);
        return button != null && button.BoundingRectangle.Top >= 0;
    }

    public void ShowDock(string anyItemName)
    {
        var bounds = MainWindow.BoundingRectangle;
        MoveMouse(new Point(bounds.Left + bounds.Width / 2, bounds.Top + 2));
        WaitUntil(() => IsDockVisible(anyItemName), "Dock wurde nicht eingeblendet.");
        // Maus auf das Dock führen, damit es geöffnet bleibt.
        MoveMouse(Center(GetDockButton(anyItemName)));
    }

    public void MoveMouseAway()
    {
        var screen = Automation.GetDesktop().BoundingRectangle;
        MoveMouse(new Point(screen.Left + screen.Width / 2, screen.Top + screen.Height * 2 / 3));
    }

    /// <summary>
    /// Setzt den Mauszeiger ohne Animation. SetCursorPos erzeugt wie Mouse.MoveTo (das intern ebenfalls
    /// nur die Position setzt) ein WM_MOUSEMOVE beim Fenster unter dem Zeiger.
    /// </summary>
    public static void MoveMouse(Point point)
    {
        Mouse.Position = point;
        Thread.Sleep(10);
    }

    public Menu OpenContextMenu(string itemName)
    {
        AutomationElement? FindMenu() => Automation.GetDesktop().FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Menu).And(cf.ByProcessId(ProcessId)));

        // Ein Rechtsklick während der Einblend-Animation geht gelegentlich verloren, daher bis zu drei Versuche.
        for (int attempt = 1; ; attempt++)
        {
            ShowDock(itemName);
            Thread.Sleep(100);
            MoveMouse(Center(GetDockButton(itemName)));
            Mouse.Click(MouseButton.Right);

            AutomationElement? menu = null;
            try
            {
                WaitUntil(() => (menu = FindMenu()) != null, "Kontextmenü wurde nicht geöffnet.", TimeSpan.FromSeconds(3));
                return menu!.AsMenu();
            }
            catch (TimeoutException) when (attempt < 3)
            {
                Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ESCAPE);
                MoveMouseAway();
                Thread.Sleep(300);
            }
        }
    }

    public void ClickContextMenuItem(string itemName, string menuItemName)
    {
        var menu = OpenContextMenu(itemName);
        AutomationElement? menuItem = null;
        WaitUntil(() => (menuItem = menu.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.MenuItem).And(cf.ByName(menuItemName)))) != null, $"Menüpunkt '{menuItemName}' fehlt.");
        menuItem!.AsMenuItem().Invoke();
    }

    public Window WaitForWindow(string title)
    {
        Window? window = null;
        WaitUntil(() => (window = FindWindow(title)) != null, $"Fenster '{title}' wurde nicht geöffnet.");
        return window!;
    }

    public bool IsWindowOpen(string title) => FindWindow(title) != null;

    // Dialoge mit Owner = Hauptfenster erscheinen in UIA als Kind des Hauptfensters, nicht als Top-Level-Fenster.
    private Window? FindWindow(string title)
    {
        foreach (var topLevel in app.GetAllTopLevelWindows(Automation))
        {
            if (topLevel.Title == title)
            {
                return topLevel;
            }

            var owned = topLevel.FindFirstDescendant(cf => cf.ByControlType(ControlType.Window).And(cf.ByName(title)));
            if (owned != null)
            {
                return owned.AsWindow();
            }
        }
        return null;
    }

    public static void InvokeButton(AutomationElement parent, string name)
    {
        AutomationElement? button = null;
        WaitUntil(() => (button = parent.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByName(name)))) != null, $"Button '{name}' fehlt.");
        button!.AsButton().Invoke();
    }

    public static Point Center(AutomationElement element)
    {
        var r = element.BoundingRectangle;
        return new Point(r.Left + r.Width / 2, r.Top + r.Height / 2);
    }

    /// <summary>Zieht mit gedrückter linker Maustaste in kleinen Schritten, damit WPF/OLE die Bewegung erkennt.</summary>
    public static void Drag(Point from, Point to)
    {
        DragStart(from);
        DragMove(from, to);
        DragEnd();
    }

    public static void DragStart(Point from)
    {
        MoveMouse(from);
        Thread.Sleep(100);
        Mouse.Down(MouseButton.Left); // wartet intern 100 ms auf die Verarbeitung
        Thread.Sleep(100);
    }

    /// <summary>
    /// Bewegt den Zeiger in Einzelschritten: Jeder Schritt erzeugt ein eigenes WM_MOUSEMOVE, das WPF (Drag-Schwelle)
    /// und die OLE-Drag-Schleife (DragEnter/DragOver) brauchen.
    /// </summary>
    public static void DragMove(Point from, Point to)
    {
        const int steps = 12;
        for (int i = 1; i <= steps; i++)
        {
            Mouse.Position = new Point(from.X + (to.X - from.X) * i / steps, from.Y + (to.Y - from.Y) * i / steps);
            // Nach dem ersten Schritt startet ggf. DoDragDrop (Drag-Ghost, modale Schleife); etwas länger warten.
            Thread.Sleep(i == 1 ? 60 : 15);
        }
        // Ziel-Rückmeldungen abwarten (Einblenden 100 ms, Lückenanimation 150 ms).
        Thread.Sleep(150);
        // Nach dem Layout-Wechsel (Quelle ausgeblendet, Lücke offen) erneut DragOver auslösen,
        // damit die Einfügeposition auf dem fertigen Layout berechnet wird.
        Mouse.Position = new Point(to.X + 1, to.Y);
        Thread.Sleep(15);
        Mouse.Position = to;
        Thread.Sleep(200);
    }

    public static void DragEnd()
    {
        Mouse.Up(MouseButton.Left); // wartet intern 100 ms auf die Verarbeitung
        Thread.Sleep(150);
    }

    public static bool StartupValueExists()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(StartupValueName) != null;
    }

    private static void RemoveStartupValue()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        key?.DeleteValue(StartupValueName, false);
    }

    public static void WaitUntil(Func<bool> condition, string message, TimeSpan? timeout = null)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < (timeout ?? DefaultTimeout))
        {
            try
            {
                if (condition())
                {
                    return;
                }
            }
            catch (Exception) when (stopwatch.Elapsed < (timeout ?? DefaultTimeout))
            {
                // UI-Elemente können während Animationen kurzzeitig ungültig sein.
            }
            Thread.Sleep(40);
        }
        throw new TimeoutException(message);
    }

    public void Dispose()
    {
        MoveMouseAway();
        try
        {
            if (!app.HasExited)
            {
                app.Kill();
            }
            Process.GetProcessById(ProcessId).WaitForExit(5000);
        }
        catch (Exception)
        {
            // Prozess ist bereits beendet.
        }
        app.Dispose();
        Automation.Dispose();
        RemoveStartupValue();

        for (int attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                Directory.Delete(DataDirectory, true);
                break;
            }
            catch (IOException)
            {
                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(100);
            }
        }
    }
}
