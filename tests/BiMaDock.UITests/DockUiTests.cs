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
    public void Kategorie_AmRechtenRandBleibtUnterDemHauptdock()
    {
        var category = TestDockItem.CategoryItem("Werkzeuge", 4);
        using var session = new BiMaDockSession(
            TestDockItem.File("Editor", Notepad, 0),
            TestDockItem.File("Konsole", Cmd, 1),
            TestDockItem.File("Dritter", Notepad, 2),
            TestDockItem.File("Vierter", Cmd, 3),
            category,
            TestDockItem.File("Inhalt A", Notepad, 0, category.Id),
            TestDockItem.File("Inhalt B", Cmd, 1, category.Id),
            TestDockItem.File("Inhalt C", Notepad, 2, category.Id));

        session.ShowDock("Werkzeuge");
        var editorBefore = session.GetDockButton("Editor").BoundingRectangle;
        var categoryButton = session.GetDockButton("Werkzeuge").BoundingRectangle;
        session.GetDockButton("Werkzeuge").AsButton().Invoke();

        BiMaDockSession.WaitUntil(() =>
            session.FindDockButton("Inhalt C") is { IsOffscreen: false },
            "Kategorie-Dock zeigt die enthaltenen Elemente nicht.");
        // Positionierung und Aufklapp-Animation abwarten
        BiMaDockSession.WaitUntil(() =>
        {
            var right = session.GetDockButton("Inhalt C").BoundingRectangle.Right;
            var left = session.GetDockButton("Inhalt A").BoundingRectangle.Left;
            return right <= categoryButton.Right + 5 && left >= editorBefore.Left - 5;
        }, "Kategorie-Dock ragt seitlich über das Hauptdock hinaus.");

        var editorAfter = session.GetDockButton("Editor").BoundingRectangle;
        Assert.InRange(editorAfter.Left - editorBefore.Left, -2, 2);
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

    /// <summary>Zieht die Daten der Quelle hinter das letzte Element des Hauptdocks.</summary>
    private static void DragFromSourceToDockEnd(BiMaDockSession session, FileDragSource source, string lastItemName)
    {
        var dock = session.MainWindow.BoundingRectangle;
        var edge = new Point(dock.Left + dock.Width / 2, dock.Top + 2);
        BiMaDockSession.DragStart(source.Center);
        BiMaDockSession.DragMove(source.Center, edge);
        BiMaDockSession.WaitUntil(() => session.IsDockVisible(lastItemName), "Dock wurde beim Ziehen nicht eingeblendet.");
        var last = session.GetDockButton(lastItemName).BoundingRectangle;
        BiMaDockSession.DragMove(edge, new Point(last.Right - 5, last.Top + last.Height / 2));
        BiMaDockSession.DragEnd();
    }

    [UiFact]
    public void DragAndDrop_LinkWirdMitHostnamenHinzugefuegt()
    {
        using var session = StartWithTwoItems();
        var screen = session.Automation.GetDesktop().BoundingRectangle;
        using var source = new FileDragSource(
            () => new System.Windows.DataObject(System.Windows.DataFormats.UnicodeText, "  https://www.example.com/seite  "),
            screen.Left + 100, screen.Top + screen.Height / 2);

        bool IsSaved() => session.ReadDockItems().Any(i => i.FilePath == "https://www.example.com/seite");
        for (int attempt = 1; attempt <= 2 && !IsSaved(); attempt++)
        {
            DragFromSourceToDockEnd(session, source, "Konsole");
            try
            {
                BiMaDockSession.WaitUntil(IsSaved, "Link wurde nicht gespeichert.", TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException) when (attempt < 2)
            {
                session.MoveMouseAway();
            }
        }

        Assert.Contains(session.ReadDockItems(), i => i.FilePath == "https://www.example.com/seite" && i.DisplayName == "example.com");
    }

    [UiFact]
    public void DragAndDrop_BeliebigerTextWirdIgnoriert()
    {
        using var session = StartWithTwoItems();
        var screen = session.Automation.GetDesktop().BoundingRectangle;
        using var source = new FileDragSource(
            () => new System.Windows.DataObject(System.Windows.DataFormats.UnicodeText, "Das ist kein Link"),
            screen.Left + 100, screen.Top + screen.Height / 2);

        // Nicht ablegbare Daten: Das Dock blendet sich beim Ziehen an den Rand gar nicht erst ein.
        session.MoveMouseAway();
        BiMaDockSession.WaitUntil(() => !session.IsDockVisible("Konsole"), "Dock ist vor dem Ziehen nicht ausgeblendet.");
        var dock = session.MainWindow.BoundingRectangle;
        var edge = new Point(dock.Left + dock.Width / 2, dock.Top + 2);
        BiMaDockSession.DragStart(source.Center);
        BiMaDockSession.DragMove(source.Center, edge);
        Thread.Sleep(500); // Einblenden beim Ziehen erfolgt ohne Verzögerung (Animation 100 ms)
        bool shownDuringDrag = session.IsDockVisible("Konsole");
        BiMaDockSession.DragEnd();

        Assert.False(shownDuringDrag, "Dock wurde für nicht ablegbaren Text eingeblendet.");
        Assert.Equal(2, session.ReadDockItems().Count);
        Assert.Null(session.FindDockButton("Das ist kein Link"));
    }

    private static BiMaDockSession StartWithThreeItems() => new(
        TestDockItem.File("Editor", Notepad, 0),
        TestDockItem.File("Konsole", Cmd, 1),
        TestDockItem.File("Dritter", Notepad, 2));

    private static List<string> MainDockOrder(BiMaDockSession session) => session.ReadDockItems()
        .Where(i => string.IsNullOrEmpty(i.Category) && !i.IsCategory)
        .OrderBy(i => i.Position)
        .Select(i => i.DisplayName)
        .ToList();

    /// <summary>Zieht ein Dock-Element auf den Bruchteil <paramref name="fraction"/> der Breite des Ziel-Elements.</summary>
    private static void DragDockItem(BiMaDockSession session, string itemName, string targetName, double fraction)
    {
        session.ShowDock(itemName);
        var from = BiMaDockSession.Center(session.GetDockButton(itemName));
        var target = session.GetDockButton(targetName).BoundingRectangle;
        var to = new Point(target.Left + (int)(target.Width * fraction), target.Top + target.Height / 2);
        BiMaDockSession.DragStart(from);
        BiMaDockSession.DragMove(from, to);
        // Lücke hat sich geöffnet; Ziel neu bestimmen, falls Nachbarn zur Seite geglitten sind
        Thread.Sleep(150);
        BiMaDockSession.DragEnd();
    }

    [UiFact]
    public void DragAndDrop_NachLinksVerschiebenLandetGenauAnZielposition()
    {
        using var session = StartWithThreeItems();

        DragDockItem(session, "Dritter", "Editor", 0.25);

        BiMaDockSession.WaitUntil(
            () => MainDockOrder(session).SequenceEqual(new[] { "Dritter", "Editor", "Konsole" }),
            $"Unerwartete Reihenfolge: {string.Join(", ", MainDockOrder(session))}");
    }

    [UiFact]
    public void DragAndDrop_NachRechtsVerschiebenLandetGenauAnZielposition()
    {
        using var session = StartWithThreeItems();

        DragDockItem(session, "Editor", "Konsole", 0.75);

        BiMaDockSession.WaitUntil(
            () => MainDockOrder(session).SequenceEqual(new[] { "Konsole", "Editor", "Dritter" }),
            $"Unerwartete Reihenfolge: {string.Join(", ", MainDockOrder(session))}");
        // Nach dem Ablegen ist das verschobene Element wieder sichtbar und keine Lücke bleibt zurück
        Assert.False(session.GetDockButton("Editor").IsOffscreen);
    }

    [UiFact]
    public void DragAndDrop_InnerhalbDerKategorieVerschiebenLandetGenauAnZielposition()
    {
        var category = TestDockItem.CategoryItem("Werkzeuge", 1);
        using var session = new BiMaDockSession(
            TestDockItem.File("Editor", Notepad, 0),
            category,
            TestDockItem.File("Eins", Notepad, 0, category.Id),
            TestDockItem.File("Zwei", Cmd, 1, category.Id),
            TestDockItem.File("Drei", Notepad, 2, category.Id));

        session.ShowDock("Werkzeuge");
        session.GetDockButton("Werkzeuge").AsButton().Invoke();
        BiMaDockSession.WaitUntil(() => session.FindDockButton("Drei") is { IsOffscreen: false }, "Kategorie wurde nicht geöffnet.");
        Thread.Sleep(450); // Aufklapp-Animation (Standard 300 ms) abwarten, bevor Positionen gelesen werden

        var from = BiMaDockSession.Center(session.GetDockButton("Eins"));
        var target = session.GetDockButton("Zwei").BoundingRectangle;
        BiMaDockSession.Drag(from, new Point(target.Left + target.Width * 3 / 4, target.Top + target.Height / 2));

        List<string> CategoryOrder() => session.ReadDockItems()
            .Where(i => i.Category == category.Id)
            .OrderBy(i => i.Position)
            .Select(i => i.DisplayName)
            .ToList();
        BiMaDockSession.WaitUntil(
            () => CategoryOrder().SequenceEqual(new[] { "Zwei", "Eins", "Drei" }),
            $"Unerwartete Reihenfolge in der Kategorie: {string.Join(", ", CategoryOrder())}");
        Assert.Equal(1, session.FindAll(FlaUI.Core.Definitions.ControlType.Button).Count(b => b.Name == "Eins"));
    }

    [UiFact]
    public void DragAndDrop_ElementVerschiebenAendertReihenfolge()
    {
        using var session = new BiMaDockSession(
            TestDockItem.File("Editor", Notepad, 0),
            TestDockItem.File("Konsole", Cmd, 1),
            TestDockItem.File("Dritter", Notepad, 2));

        // Nicht an den äußersten Rand zielen: Beim Einklappen der Quelle rutscht "Dritter" nach links.
        DragDockItem(session, "Editor", "Dritter", 0.75);

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

    [UiFact]
    public void Kontextmenue_Aufraeumen_EntferntVerwaistesElementUndMachtEsRueckgaengig()
    {
        var missing = TestDockItem.File("Verwaist", @"C:\BiMaDock-UITest\fehlt.exe", 1);
        using var session = new BiMaDockSession(
            TestDockItem.File("Editor", Notepad, 0),
            missing);
        string backupPath = Path.Combine(session.DataDirectory, "docksettings.backup.json");

        session.ClickContextMenuItem("Editor", "Aufräumen …");
        var dialog = session.WaitForWindow("Dock aufräumen");
        var checkBox = dialog.FindFirstDescendant(cf => cf.ByAutomationId("CleanupItem_" + missing.Id));
        Assert.NotNull(checkBox);
        Assert.Equal(ToggleState.On, checkBox.AsCheckBox().ToggleState);
        Assert.Null(dialog.FindFirstDescendant(cf => cf.ByName("Editor").And(cf.ByControlType(ControlType.CheckBox))));
        BiMaDockSession.InvokeButton(dialog, "Ausgewählte entfernen");

        BiMaDockSession.WaitUntil(() => session.FindDockButton("Verwaist") == null, "Verwaistes Element ist noch im Dock.");
        BiMaDockSession.WaitUntil(
            () => session.ReadDockItems().All(i => i.DisplayName != "Verwaist") && File.Exists(backupPath),
            "Element wurde nicht entfernt oder keine Sicherung angelegt.");
        Assert.NotNull(session.FindDockButton("Editor"));

        session.ClickContextMenuItem("Editor", "Aufräumen rückgängig machen");
        BiMaDockSession.InvokeButton(session.WaitForWindow("Aufräumen rückgängig machen"), "OK");

        BiMaDockSession.WaitUntil(() => session.FindDockButton("Verwaist") != null, "Element wurde nicht wiederhergestellt.");
        BiMaDockSession.WaitUntil(
            () => session.ReadDockItems().Any(i => i.DisplayName == "Verwaist") && !File.Exists(backupPath),
            "Wiederherstellung wurde nicht gespeichert oder Sicherung nicht entfernt.");
    }

    [UiFact]
    public void Dialog_LaesstSichAnFreierFlaecheVerschieben()
    {
        using var session = StartWithTwoItems();

        session.ClickContextMenuItem("Editor", "Über");
        var about = session.WaitForWindow("Über BiMaDock");
        var before = about.BoundingRectangle;
        // Freie Fläche oben links auf der Dialogoberfläche (innerhalb von Rand und Innenabstand)
        var from = new Point(before.Left + 20, before.Top + 20);
        BiMaDockSession.Drag(from, new Point(from.X + 150, from.Y + 100));

        BiMaDockSession.WaitUntil(() =>
        {
            var after = about.BoundingRectangle;
            return Math.Abs(after.Left - before.Left - 150) <= 10 && Math.Abs(after.Top - before.Top - 100) <= 10;
        }, "Dialog wurde nicht verschoben.");
        BiMaDockSession.InvokeButton(about, "Schließen");
    }

    [UiFact]
    public void Kontextmenue_Aufraeumen_MeldetSaubereDock()
    {
        using var session = StartWithTwoItems();

        session.ClickContextMenuItem("Editor", "Aufräumen …");
        BiMaDockSession.InvokeButton(session.WaitForWindow("Dock aufräumen"), "OK");

        BiMaDockSession.WaitUntil(() => !session.IsWindowOpen("Dock aufräumen"), "Meldung wurde nicht geschlossen.");
        Assert.Equal(2, session.ReadDockItems().Count);
    }
}
