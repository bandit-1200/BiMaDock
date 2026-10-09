using System.IO;
using System.Drawing;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Xunit;

namespace BiMaDock.UITests;

/// <summary>
/// End-to-End-Tests der Oberfläche. Jeder Test startet BiMaDock mit eigenem Testdatenordner.
/// </summary>
public sealed class DockUiTests
{
    private const string Notepad = @"C:\Windows\System32\notepad.exe";
    private const string Cmd = @"C:\Windows\System32\cmd.exe";

    private static BiMaDockSession StartWithTwoItems() => new(
        TestDockItem.File("Editor", Notepad, 0),
        TestDockItem.File("Konsole", Cmd, 1));

    [UiFact]
    public void Start_ZeigtGespeicherteElemente()
    {
        using var session = StartWithTwoItems();

        Assert.NotNull(session.GetDockButton("Editor"));
        Assert.NotNull(session.GetDockButton("Konsole"));
    }

    [UiFact]
    public void Start_FehlendeIcondateiVerhindertNichtDasLadenWeitererElemente()
    {
        var broken = TestDockItem.File("Kaputtes Icon", @"C:\BiMaDock-UITest\fehlt.png", 0);
        broken.IconSource = @"C:\BiMaDock-UITest\fehlt.png";
        using var session = new BiMaDockSession(
            broken,
            TestDockItem.File("Editor", Notepad, 1));

        Assert.NotNull(session.GetDockButton("Kaputtes Icon"));
        Assert.NotNull(session.GetDockButton("Editor"));
    }

    [UiFact]
    public void ErsterStart_SpeichertStandardelemente()
    {
        using var session = new BiMaDockSession();

        Assert.NotNull(session.GetDockButton("File Explorer"));
        BiMaDockSession.WaitUntil(
            () => File.Exists(session.DockSettingsPath) && session.ReadDockItems().Count == 2,
            "Standardelemente wurden beim ersten Start nicht gespeichert.");
    }

    [UiFact]
    public void Dock_BlendetBeiMauskontaktEinUndWiederAus()
    {
        using var session = StartWithTwoItems();
        session.MoveMouseAway();
        BiMaDockSession.WaitUntil(() => !session.IsDockVisible("Editor"), "Dock ist nach dem Start nicht ausgeblendet.");

        for (int round = 0; round < 2; round++)
        {
            session.ShowDock("Editor");
            Assert.True(session.IsDockVisible("Editor"));

            session.MoveMouseAway();
            BiMaDockSession.WaitUntil(() => !session.IsDockVisible("Editor"), "Dock wurde nicht wieder ausgeblendet.");
        }
    }

    [UiFact]
    public void Kontextmenue_KategorieErstellen_LegtKategorieAn()
    {
        using var session = StartWithTwoItems();

        session.ClickContextMenuItem("Editor", "Kategorie erstellen");
        var dialog = session.WaitForWindow("Kategorie erstellen");
        dialog.FindFirstDescendant(cf => cf.ByAutomationId("CategoryNameTextBox")).AsTextBox().Text = "UITest Kategorie";
        BiMaDockSession.InvokeButton(dialog, "OK");

        Assert.NotNull(session.GetDockButton("UITest Kategorie"));
        BiMaDockSession.WaitUntil(
            () => session.ReadDockItems().Any(i => i.IsCategory && i.DisplayName == "UITest Kategorie"),
            "Kategorie wurde nicht gespeichert.");
    }

    [UiFact]
    public void Kategorie_KlickZeigtEnthalteneElemente()
    {
        var category = TestDockItem.CategoryItem("Werkzeuge", 1);
        using var session = new BiMaDockSession(
            TestDockItem.File("Editor", Notepad, 0),
            category,
            TestDockItem.File("Rechner im Ordner", Notepad, 0, category.Id));

        Assert.Null(session.FindDockButton("Rechner im Ordner"));

        session.ShowDock("Werkzeuge");
        session.GetDockButton("Werkzeuge").AsButton().Invoke();

        BiMaDockSession.WaitUntil(() =>
            session.FindDockButton("Rechner im Ordner") is { IsOffscreen: false },
            "Kategorie-Dock zeigt das enthaltene Element nicht.");
    }

    [UiFact]
    public void Kontextmenue_EigenschaftenBearbeiten_BenenntElementUm()
    {
        using var session = StartWithTwoItems();

        session.ClickContextMenuItem("Editor", "Eigenschaften bearbeiten");
        var editWindow = session.WaitForWindow("Eigenschaften bearbeiten");
        editWindow.FindFirstDescendant(cf => cf.ByAutomationId("NameTextBox")).AsTextBox().Text = "Notizen";
        BiMaDockSession.InvokeButton(editWindow, "Speichern");

        Assert.NotNull(session.GetDockButton("Notizen"));
        BiMaDockSession.WaitUntil(
            () => session.ReadDockItems().Any(i => i.DisplayName == "Notizen" && i.FilePath == Notepad),
            "Neuer Name wurde nicht gespeichert.");
    }

    [UiFact]
    public void Kontextmenue_Loeschen_EntferntElementNachBestaetigung()
    {
        using var session = StartWithTwoItems();

        session.ClickContextMenuItem("Konsole", "Löschen");
        BiMaDockSession.InvokeButton(session.WaitForWindow("Bestätigungsdialog"), "Ja");

        BiMaDockSession.WaitUntil(() => session.FindDockButton("Konsole") == null, "Element ist noch im Dock.");
        BiMaDockSession.WaitUntil(
            () => session.ReadDockItems().All(i => i.DisplayName != "Konsole"),
            "Element ist noch gespeichert.");
        Assert.NotNull(session.FindDockButton("Editor"));
    }

    [UiFact]
    public void Kontextmenue_Loeschen_AbbrechenBehaeltElement()
    {
        using var session = StartWithTwoItems();

        session.ClickContextMenuItem("Konsole", "Löschen");
        BiMaDockSession.InvokeButton(session.WaitForWindow("Bestätigungsdialog"), "Nein");

        BiMaDockSession.WaitUntil(() => !session.IsWindowOpen("Bestätigungsdialog"), "Dialog wurde nicht geschlossen.");
        Assert.NotNull(session.FindDockButton("Konsole"));
        Assert.Contains(session.ReadDockItems(), i => i.DisplayName == "Konsole");
    }

    [UiFact]
    public void Einstellungen_SpeichernSchreibtEinblendverzoegerung()
    {
        using var session = StartWithTwoItems();

        session.ClickContextMenuItem("Editor", "Einstellungen");
        var settings = session.WaitForWindow("Einstellungen");
        settings.FindFirstDescendant(cf => cf.ByAutomationId("DockShowDelaySlider")).AsSlider().Value = 700;
        BiMaDockSession.InvokeButton(settings, "Speichern");

        BiMaDockSession.WaitUntil(() =>
        {
            if (!File.Exists(session.StyleSettingsPath))
            {
                return false;
            }
            using var json = JsonDocument.Parse(File.ReadAllText(session.StyleSettingsPath));
            return json.RootElement.TryGetProperty("DockShowDelayMilliseconds", out var delay) && delay.GetInt32() == 700;
        }, "Einblendverzögerung wurde nicht gespeichert.");
        BiMaDockSession.WaitUntil(() => !session.IsWindowOpen("Einstellungen"), "Einstellungen wurden nicht geschlossen.");
    }

    [UiFact]
    public void UeberFenster_OeffnetUndSchliesst()
    {
        using var session = StartWithTwoItems();

        session.ClickContextMenuItem("Editor", "Über");
        var about = session.WaitForWindow("Über BiMaDock");
        BiMaDockSession.InvokeButton(about, "Schließen");

        BiMaDockSession.WaitUntil(() => !session.IsWindowOpen("Über BiMaDock"), "Über-Fenster wurde nicht geschlossen.");
    }

    [UiFact]
    public void Autostart_UmschaltenSchreibtUndEntferntRegistryWert()
    {
        using var session = StartWithTwoItems();
        Assert.False(BiMaDockSession.StartupValueExists());

        session.ClickContextMenuItem("Editor", "Autostart");
        BiMaDockSession.WaitUntil(BiMaDockSession.StartupValueExists, "Autostart wurde nicht aktiviert.");

        session.ClickContextMenuItem("Editor", "Autostart");
        BiMaDockSession.WaitUntil(() => !BiMaDockSession.StartupValueExists(), "Autostart wurde nicht deaktiviert.");
    }

    [UiFact]
    public void DragAndDrop_DateiAusExplorerQuelleWirdHinzugefuegt()
    {
        string droppedFile = Path.Combine(Path.GetTempPath(), $"BiMaDock-UITest-{Guid.NewGuid():N}.txt");
        File.WriteAllText(droppedFile, "UI-Test");
        try
        {
            using var session = StartWithTwoItems();
            var screen = session.Automation.GetDesktop().BoundingRectangle;
            using var dragSource = new FileDragSource(droppedFile, screen.Left + 100, screen.Top + screen.Height / 2);

            bool IsSaved() => session.ReadDockItems()
                .Any(i => string.Equals(i.FilePath, droppedFile, StringComparison.OrdinalIgnoreCase));

            // Simulierte OLE-Drags hängen vom Timing ab; ein zweiter Versuch fängt gelegentliche Aussetzer ab.
            for (int attempt = 1; attempt <= 2 && !IsSaved(); attempt++)
            {
                // Erst den oberen Rand anfahren (blendet das Dock ein), dann hinter das letzte Element ziehen.
                var dock = session.MainWindow.BoundingRectangle;
                var edge = new Point(dock.Left + dock.Width / 2, dock.Top + 2);
                BiMaDockSession.DragStart(dragSource.Center);
                BiMaDockSession.DragMove(dragSource.Center, edge);
                BiMaDockSession.WaitUntil(() => session.IsDockVisible("Konsole"), "Dock wurde beim Ziehen nicht eingeblendet.");
                var last = session.GetDockButton("Konsole").BoundingRectangle;
                BiMaDockSession.DragMove(edge, new Point(last.Right - 5, last.Top + last.Height / 2));
                BiMaDockSession.DragEnd();

                try
                {
                    BiMaDockSession.WaitUntil(IsSaved, "Gezogene Datei wurde nicht gespeichert.", TimeSpan.FromSeconds(5));
                }
                catch (TimeoutException) when (attempt < 2)
                {
                    session.MoveMouseAway();
                }
            }

            Assert.True(IsSaved(), "Gezogene Datei wurde nicht gespeichert.");
        }
        finally
        {
            File.Delete(droppedFile);
        }
    }

    [UiFact]
    public void DragAndDrop_ElementVerschiebenAendertReihenfolge()
    {
        using var session = new BiMaDockSession(
            TestDockItem.File("Editor", Notepad, 0),
            TestDockItem.File("Konsole", Cmd, 1),
            TestDockItem.File("Dritter", Notepad, 2));

        session.ShowDock("Editor");
        var from = BiMaDockSession.Center(session.GetDockButton("Editor"));
        var dritter = session.GetDockButton("Dritter").BoundingRectangle;
        BiMaDockSession.Drag(from, new Point(dritter.Right - 5, dritter.Top + dritter.Height / 2));

        BiMaDockSession.WaitUntil(() =>
        {
            var order = session.ReadDockItems()
                .Where(i => string.IsNullOrEmpty(i.Category) && !i.IsCategory)
                .OrderBy(i => i.Position)
                .Select(i => i.DisplayName)
                .ToList();
            return order.IndexOf("Editor") > order.IndexOf("Konsole");
        }, "Reihenfolge wurde nach dem Verschieben nicht gespeichert.");
    }

    [UiFact]
    public void Kontextmenue_Beenden_SchliesstAnwendungNachBestaetigung()
    {
        using var session = StartWithTwoItems();

        session.ClickContextMenuItem("Editor", "Beenden");
        BiMaDockSession.InvokeButton(session.WaitForWindow("Bestätigungsdialog"), "Ja");

        BiMaDockSession.WaitUntil(() => session.HasExited, "BiMaDock wurde nicht beendet.");
        Assert.Contains(session.ReadDockItems(), i => i.DisplayName == "Editor");
    }
}
