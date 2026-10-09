using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BiMaDock;

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

    // Methode zum Laden von SelectedEffectIndex und den Effekt-Einstellungen
    public static void LoadSettings()
    {
        var settings = StyleSettingsFile.Load();
        if (settings == null)
        {
            return;
        }

        if (settings.SelectedEffectIndex is int selectedEffectIndex)
        {
            SelectedEffectIndex = selectedEffectIndex;
        }

        // Lade die Einstellungen für Scale
        if (settings.Scale is { } scale)
        {
            if (scale.Duration is double duration) ScaleSettings.Duration = duration;
            if (scale.ScaleFactor is double scaleFactor) ScaleSettings.ScaleFactor = scaleFactor;
            if (scale.AutoReverse is bool autoReverse) ScaleSettings.AutoReverse = autoReverse;
            if (scale.EffectIndex is int effectIndex) ScaleSettings.EffectIndex = effectIndex;
        }

        // Lade die Einstellungen für Rotate
        if (settings.Rotate is { } rotate)
        {
            if (rotate.Duration is double duration) RotateSettings.Duration = duration;
            if (rotate.Angle is double angle) RotateSettings.Angle = angle;
            if (rotate.AutoReverse is bool autoReverse) RotateSettings.AutoReverse = autoReverse;
            if (rotate.EffectIndex is int effectIndex) RotateSettings.EffectIndex = effectIndex;
        }

        // Lade die Einstellungen für Translate
        if (settings.Translate is { } translate)
        {
            if (translate.Duration is double duration) TranslateSettings.Duration = duration;
            if (translate.TranslateX is double translateX) TranslateSettings.TranslateX = translateX;
            if (translate.TranslateY is double translateY) TranslateSettings.TranslateY = translateY;
            if (translate.AutoReverse is bool autoReverse) TranslateSettings.AutoReverse = autoReverse;
            if (translate.EffectIndex is int effectIndex) TranslateSettings.EffectIndex = effectIndex;
        }

        if (settings.Swing is { } swing)
        {
            if (swing.Duration is double duration) SwingSettings.Duration = duration;
            if (swing.Angle is double angle) SwingSettings.Angle = angle;
        }
    }

    /// <summary>
    /// Liefert die wiederverwendete Transform-Gruppe (Scale, Rotate, Translate) des Buttons.
    /// Sie wird nur einmal pro Button angelegt, damit laufende Animationen nicht durch einen
    /// neuen RenderTransform abgeschnitten werden.
    /// </summary>
    internal static TransformGroup GetHoverTransform(Button button, Point origin)
    {
        button.RenderTransformOrigin = origin;

        if (button.RenderTransform is TransformGroup existing
            && existing.Children.Count == 3
            && existing.Children[0] is ScaleTransform
            && existing.Children[1] is RotateTransform
            && existing.Children[2] is TranslateTransform)
        {
            return existing;
        }

        var group = new TransformGroup();
        group.Children.Add(new ScaleTransform(1.0, 1.0));
        group.Children.Add(new RotateTransform(0));
        group.Children.Add(new TranslateTransform(0, 0));
        button.RenderTransform = group;
        return group;
    }

    private static DoubleAnimation CreateAnimation(double from, double to, double durationSeconds, bool autoReverse, double repeatCount)
    {
        // FillBehavior.Stop: Nach Ende der Animation kehrt der Wert immer zum Ruhewert zurück.
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = new Duration(TimeSpan.FromSeconds(durationSeconds)),
            AutoReverse = autoReverse,
            RepeatBehavior = new RepeatBehavior(repeatCount),
            FillBehavior = FillBehavior.Stop
        };
        animation.Freeze();
        return animation;
    }

    // Animationen
    public static void AnimatScaleTransform(Button button)
    {
        var scaleTransform = (ScaleTransform)GetHoverTransform(button, new Point(0.5, 0.5)).Children[0];
        var scaleAnimation = CreateAnimation(1.0, ScaleSettings.ScaleFactor, ScaleSettings.Duration, ScaleSettings.AutoReverse, 2);

        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation, HandoffBehavior.SnapshotAndReplace);
        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation, HandoffBehavior.SnapshotAndReplace);
    }

    public static void AnimatRotateTransform(Button button)
    {
        var rotateTransform = (RotateTransform)GetHoverTransform(button, new Point(0.5, 0.5)).Children[1];
        var rotateAnimation = CreateAnimation(0, RotateSettings.Angle, RotateSettings.Duration, RotateSettings.AutoReverse, 1);

        rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation, HandoffBehavior.SnapshotAndReplace);
    }

    public static void AnimatTranslateTransform(Button button)
    {
        var translateTransform = (TranslateTransform)GetHoverTransform(button, new Point(0.5, 0.5)).Children[2];
        var translateXAnimation = CreateAnimation(0, TranslateSettings.TranslateX, TranslateSettings.Duration, TranslateSettings.AutoReverse, 1);
        var translateYAnimation = CreateAnimation(0, TranslateSettings.TranslateY, TranslateSettings.Duration, TranslateSettings.AutoReverse, 1);

        // Beginnt die Animation für X und Y Achsen
        translateTransform.BeginAnimation(TranslateTransform.XProperty, translateXAnimation, HandoffBehavior.SnapshotAndReplace);
        translateTransform.BeginAnimation(TranslateTransform.YProperty, translateYAnimation, HandoffBehavior.SnapshotAndReplace);
    }

    public static void AnimatSwingTransform(Button button)
    {
        // Ursprung oben mitte
        var rotateTransform = (RotateTransform)GetHoverTransform(button, new Point(0.5, 0.0)).Children[1];

        var swingAnimation = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
        double[] angleFactors = { 0, 1, -1, 2d / 3, -2d / 3, 1d / 3, -1d / 3, 0 };
        for (int index = 0; index < angleFactors.Length; index++)
        {
            double progress = index / (double)(angleFactors.Length - 1);
            double angle = SwingSettings.Angle * angleFactors[index];
            var keyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(SwingSettings.Duration * progress));
            swingAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(angle, keyTime));
        }
        swingAnimation.Freeze();

        rotateTransform.BeginAnimation(RotateTransform.AngleProperty, swingAnimation, HandoffBehavior.SnapshotAndReplace);
    }

    // Auswahl der Animation
    public static void AnimateButtonByChoice(Button button)
    {
        switch (SelectedEffectIndex)
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
