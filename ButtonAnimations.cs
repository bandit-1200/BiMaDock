using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Newtonsoft.Json;

public class ButtonAnimations
{
        public class EffectSettings
{
    public double Duration { get; set; }
    public double ScaleFactor { get; set; }  // Nur für Scale
    public double Angle { get; set; }        // Nur für Rotate
    public double TranslateX { get; set; }   // Nur für Translate
    public double TranslateY { get; set; }   // Nur für Translate
    public bool AutoReverse { get; set; }
    public int EffectIndex { get; set; }
}

    private static int SelectedEffectIndex = 0;
    public static EffectSettings ScaleSettings = new EffectSettings();
    public static EffectSettings RotateSettings = new EffectSettings();
    public static EffectSettings TranslateSettings = new EffectSettings();




    // Methode zum Laden von SelectedEffectIndex
public static void LoadSettings()
{
    Console.WriteLine("Einstellungen werden geladen...");

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

            // Lade den SelectedEffectIndex
            if (settings?.SelectedEffectIndex != null)
            {
                SelectedEffectIndex = (int)settings.SelectedEffectIndex;
                Console.WriteLine($"SelectedEffectIndex geladen: {SelectedEffectIndex}");
            }

            // Lade die Einstellungen für Scale
            if (settings?.Scale != null)
            {
                if (settings.Scale?.Duration != null) ScaleSettings.Duration = (double)settings.Scale.Duration;
                if (settings.Scale?.ScaleFactor != null) ScaleSettings.ScaleFactor = (double)settings.Scale.ScaleFactor;
                if (settings.Scale?.AutoReverse != null) ScaleSettings.AutoReverse = (bool)settings.Scale.AutoReverse;
                if (settings.Scale?.EffectIndex != null) ScaleSettings.EffectIndex = (int)settings.Scale.EffectIndex;

                Console.WriteLine($"Scale Einstellungen geladen: Duration={ScaleSettings.Duration}, ScaleFactor={ScaleSettings.ScaleFactor}, AutoReverse={ScaleSettings.AutoReverse}");
            }

            // Lade die Einstellungen für Rotate
            if (settings?.Rotate != null)
            {
                if (settings.Rotate?.Duration != null) RotateSettings.Duration = (double)settings.Rotate.Duration;
                if (settings.Rotate?.Angle != null) RotateSettings.Angle = (double)settings.Rotate.Angle;
                if (settings.Rotate?.AutoReverse != null) RotateSettings.AutoReverse = (bool)settings.Rotate.AutoReverse;
                if (settings.Rotate?.EffectIndex != null) RotateSettings.EffectIndex = (int)settings.Rotate.EffectIndex;

                Console.WriteLine($"Rotate Einstellungen geladen: Duration={RotateSettings.Duration}, Angle={RotateSettings.Angle}, AutoReverse={RotateSettings.AutoReverse}");
            }

            // Lade die Einstellungen für Translate
            if (settings?.Translate != null)
            {
                if (settings.Translate?.Duration != null) TranslateSettings.Duration = (double)settings.Translate.Duration;
                if (settings.Translate?.TranslateX != null) TranslateSettings.TranslateX = (double)settings.Translate.TranslateX;
                if (settings.Translate?.TranslateY != null) TranslateSettings.TranslateY = (double)settings.Translate.TranslateY;
                if (settings.Translate?.AutoReverse != null) TranslateSettings.AutoReverse = (bool)settings.Translate.AutoReverse;
                if (settings.Translate?.EffectIndex != null) TranslateSettings.EffectIndex = (int)settings.Translate.EffectIndex;

                Console.WriteLine($"Translate Einstellungen geladen: Duration={TranslateSettings.Duration}, TranslateX={TranslateSettings.TranslateX}, TranslateY={TranslateSettings.TranslateY}, AutoReverse={TranslateSettings.AutoReverse}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Laden der Einstellungen: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine("Einstellungsdatei nicht gefunden, Standardwerte werden verwendet.");
    }
}

    // Animationen
    public static void NotAnimate(Button button)
    {
        // Keine Animation, leer lassen
    }


// ScaleTransform_Animation
    // public static void AnimatScaleTransform(Button button)
    // {
    //     var scaleTransform = new ScaleTransform(1.0, 1.0);
    //     button.RenderTransformOrigin = new Point(0.5, 0.5);
    //     button.RenderTransform = scaleTransform;

    //     var scaleXAnimation = new DoubleAnimation
    //     {
    //         From = 1.0,
    //         To = 1.2,
    //         Duration = new Duration(TimeSpan.FromSeconds(0.3)),
    //         AutoReverse = true,
    //         RepeatBehavior = new RepeatBehavior(2)
    //     };

    //     var scaleYAnimation = new DoubleAnimation
    //     {
    //         From = 1.0,
    //         To = 1.2,
    //         Duration = new Duration(TimeSpan.FromSeconds(0.3)),
    //         AutoReverse = true,
    //         RepeatBehavior = new RepeatBehavior(2)
    //     };

    //     scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
    //     scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
    // }

public static void AnimatScaleTransform(Button button)
{
    var scaleTransform = new ScaleTransform(1.0, 1.0);
    button.RenderTransformOrigin = new Point(0.5, 0.5);
    button.RenderTransform = scaleTransform;

    var scaleXAnimation = new DoubleAnimation
    {
        From = 1.0,
        To = ScaleSettings.ScaleFactor, // Verwendet die ScaleFactor-Variable
        Duration = new Duration(TimeSpan.FromSeconds(ScaleSettings.Duration)), // Verwendet die Duration-Variable
        AutoReverse = ScaleSettings.AutoReverse, // Verwendet AutoReverse-Variable
        RepeatBehavior = new RepeatBehavior(2)
    };

    var scaleYAnimation = new DoubleAnimation
    {
        From = 1.0,
        To = ScaleSettings.ScaleFactor, // Verwendet die ScaleFactor-Variable
        Duration = new Duration(TimeSpan.FromSeconds(ScaleSettings.Duration)), // Verwendet die Duration-Variable
        AutoReverse = ScaleSettings.AutoReverse, // Verwendet AutoReverse-Variable
        RepeatBehavior = new RepeatBehavior(2)
    };

    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
}




    public static void AnimatRotateTransform(Button button)
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
    public static void AnimateButtonByChoice(Button button)
    {
        // int? choice = SelectedEffectIndex;
        int animationChoice = SelectedEffectIndex;
        Console.WriteLine($"AnimateButtonByChoice aufgerufen mit Button: {button.Name}, Choice: {animationChoice}");

        switch (animationChoice)
        {
            case 1:
                Console.WriteLine("Starte AnimateButton");
                AnimatScaleTransform(button);
                break;
            case 2:
                Console.WriteLine("Starte AnimateButton2");
                AnimatRotateTransform(button);
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