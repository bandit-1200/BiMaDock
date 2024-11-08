using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Newtonsoft.Json;

public class ButtonAnimations
{
    // private static readonly string settingsFilePath = "Pfad/zur/StyleSettings.json";
    private static int SelectedEffectIndex = 0;


    // Methode zum Laden von SelectedEffectIndex
public static void LoadSelectedEffectIndex()
{
    Console.WriteLine("SelectedEffectIndex geladen:");

    // Hole den Pfad zum AppData\Local\BiMaDock Ordner
    string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    string directoryPath = Path.Combine(appDataPath, "BiMaDock");
    string settingsFilePath = Path.Combine(directoryPath, "StyleSettings.json");

    // Überprüfe, ob die Datei existiert
    if (File.Exists(settingsFilePath))
    {
        try
        {
            string json = File.ReadAllText(settingsFilePath);
            var settings = JsonConvert.DeserializeObject<dynamic>(json);

            // Nutze die dynamische Abfrage und verwende eine explizite Prüfung
            if (settings?.SelectedEffectIndex != null)
            {
                SelectedEffectIndex = (int)settings.SelectedEffectIndex;
                Console.WriteLine($"SelectedEffectIndex geladen: {SelectedEffectIndex}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Laden der Einstellungen: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine("Einstellungsdatei nicht gefunden, Standardwert wird verwendet.");
    }
}

    // Animationen
    public static void NotAnimate(Button button)
    {
        // Keine Animation, leer lassen
    }

    public static void AnimateButton(Button button)
    {
        var scaleTransform = new ScaleTransform(1.0, 1.0);
        button.RenderTransformOrigin = new Point(0.5, 0.5);
        button.RenderTransform = scaleTransform;

        var scaleXAnimation = new DoubleAnimation
        {
            From = 1.0,
            To = 1.2,
            Duration = new Duration(TimeSpan.FromSeconds(0.3)),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(2)
        };

        var scaleYAnimation = new DoubleAnimation
        {
            From = 1.0,
            To = 1.2,
            Duration = new Duration(TimeSpan.FromSeconds(0.3)),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(2)
        };

        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
    }

    public static void AnimateButton2(Button button)
    {
        var rotateTransform = new RotateTransform();
        button.RenderTransformOrigin = new Point(0.5, 0.5);
        button.RenderTransform = rotateTransform;

        var rotateAnimation = new DoubleAnimation
        {
            From = 0,
            To = 360,
            Duration = new Duration(TimeSpan.FromSeconds(1)),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(1)
        };

        rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
    }

    // Auswahl der Animation
    public static void AnimateButtonByChoice(Button button, int? choice = null)
    {
        int animationChoice = choice ?? SelectedEffectIndex;
        Console.WriteLine($"AnimateButtonByChoice aufgerufen mit Button: {button.Name}, Choice: {animationChoice}");

        switch (animationChoice)
        {
            case 1:
                Console.WriteLine("Starte AnimateButton");
                AnimateButton(button);
                break;
            case 2:
                Console.WriteLine("Starte AnimateButton2");
                AnimateButton2(button);
                break;
            default:
                Console.WriteLine("Keine Animation - NotAnimate wird aufgerufen");
                NotAnimate(button);
                break;
        }
    }
}




// // Beispiel für den Start der Anwendung:
// ButtonAnimations.LoadSelectedEffectIndex();

// // Anwendung der Animation auf einen Button
// ButtonAnimations.AnimateButtonByChoice(myButton);