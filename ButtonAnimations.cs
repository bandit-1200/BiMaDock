using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Newtonsoft.Json;
using System.Diagnostics;

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
    public static EffectSettings SwingSettings = new EffectSettings { Duration = 3.5, Angle = 30 };




    // Methode zum Laden von SelectedEffectIndex
    public static void LoadSettings()
    {

        // Hole den Pfad zum AppData\Local\BiMaDock Ordner
        string settingsFilePath = Path.Combine(BiMaDock.AppPaths.AppDataDirectory, "StyleSettings.json");

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
                }

                // Lade die Einstellungen für Scale
                if (settings?.Scale != null)
                {
                    if (settings.Scale?.Duration != null) ScaleSettings.Duration = (double)settings.Scale.Duration;
                    if (settings.Scale?.ScaleFactor != null) ScaleSettings.ScaleFactor = (double)settings.Scale.ScaleFactor;
                    if (settings.Scale?.AutoReverse != null) ScaleSettings.AutoReverse = (bool)settings.Scale.AutoReverse;
                    if (settings.Scale?.EffectIndex != null) ScaleSettings.EffectIndex = (int)settings.Scale.EffectIndex;

                }

                // Lade die Einstellungen für Rotate
                if (settings?.Rotate != null)
                {
                    if (settings.Rotate?.Duration != null) RotateSettings.Duration = (double)settings.Rotate.Duration;
                    if (settings.Rotate?.Angle != null) RotateSettings.Angle = (double)settings.Rotate.Angle;
                    if (settings.Rotate?.AutoReverse != null) RotateSettings.AutoReverse = (bool)settings.Rotate.AutoReverse;
                    if (settings.Rotate?.EffectIndex != null) RotateSettings.EffectIndex = (int)settings.Rotate.EffectIndex;

                }

                // Lade die Einstellungen für Translate
                if (settings?.Translate != null)
                {
                    if (settings.Translate?.Duration != null) TranslateSettings.Duration = (double)settings.Translate.Duration;
                    if (settings.Translate?.TranslateX != null) TranslateSettings.TranslateX = (double)settings.Translate.TranslateX;
                    if (settings.Translate?.TranslateY != null) TranslateSettings.TranslateY = (double)settings.Translate.TranslateY;
                    if (settings.Translate?.AutoReverse != null) TranslateSettings.AutoReverse = (bool)settings.Translate.AutoReverse;
                    if (settings.Translate?.EffectIndex != null) TranslateSettings.EffectIndex = (int)settings.Translate.EffectIndex;

                }

                if (settings?.Swing != null)
                {
                    if (settings.Swing?.Duration != null) SwingSettings.Duration = (double)settings.Swing.Duration;
                    if (settings.Swing?.Angle != null) SwingSettings.Angle = (double)settings.Swing.Angle;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden der Einstellungen: {ex.Message}");
            }
        }
    }

    // Animationen
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
            To = RotateSettings.Angle, // Verwendet die Angle-Variable
            Duration = new Duration(TimeSpan.FromSeconds(RotateSettings.Duration)), // Verwendet die Duration-Variable
            AutoReverse = RotateSettings.AutoReverse, // Verwendet AutoReverse-Variable
            RepeatBehavior = new RepeatBehavior(1)
        };

        rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
    }
    public static void AnimatTranslateTransform(Button button)
    {
        var translateTransform = new TranslateTransform();
        button.RenderTransformOrigin = new Point(0.5, 0.5);
        button.RenderTransform = translateTransform;

        var translateXAnimation = new DoubleAnimation
        {
            From = 0,
            To = TranslateSettings.TranslateX, // Verwendet TranslateX aus den Einstellungen
            Duration = new Duration(TimeSpan.FromSeconds(TranslateSettings.Duration)), // Verwendet die Duration
            AutoReverse = TranslateSettings.AutoReverse, // Verwendet AutoReverse aus den Einstellungen
            RepeatBehavior = new RepeatBehavior(1)
        };

        var translateYAnimation = new DoubleAnimation
        {
            From = 0,
            To = TranslateSettings.TranslateY, // Verwendet TranslateY aus den Einstellungen
            Duration = new Duration(TimeSpan.FromSeconds(TranslateSettings.Duration)), // Verwendet die Duration
            AutoReverse = TranslateSettings.AutoReverse, // Verwendet AutoReverse aus den Einstellungen
            RepeatBehavior = new RepeatBehavior(1)
        };

        // Beginnt die Animation für X und Y Achsen
        translateTransform.BeginAnimation(TranslateTransform.XProperty, translateXAnimation);
        translateTransform.BeginAnimation(TranslateTransform.YProperty, translateYAnimation);
    }


    public static void AnimatSwingTransform(Button button)
    {
        var rotateTransform = new RotateTransform();
        button.RenderTransformOrigin = new Point(0.5, 0.0); // Ursprung oben mitte
        button.RenderTransform = rotateTransform;

        var swingAnimation = new DoubleAnimationUsingKeyFrames();
        double[] angleFactors = { 0, 1, -1, 2d / 3, -2d / 3, 1d / 3, -1d / 3, 0 };
        for (int index = 0; index < angleFactors.Length; index++)
        {
            double progress = index / (double)(angleFactors.Length - 1);
            double angle = SwingSettings.Angle * angleFactors[index];
            var keyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(SwingSettings.Duration * progress));
            swingAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(angle, keyTime));
        }

        // Füge die Animation hinzu
        rotateTransform.BeginAnimation(RotateTransform.AngleProperty, swingAnimation);
    }


    // Auswahl der Animation
    public static void AnimateButtonByChoice(Button button)
    {
        int animationChoice = SelectedEffectIndex;

        switch (animationChoice)
        {
            case 1:
                AnimatScaleTransform(button);
                break;
            case 2:
                AnimatRotateTransform(button);
                break;

            case 3:
                AnimatTranslateTransform(button);
                break;
            case 4:
                AnimatSwingTransform(button);
                break;



            default:
                break;
        }
    }
}