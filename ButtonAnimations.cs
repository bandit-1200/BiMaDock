using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

public class ButtonAnimations
{


    public static void NotAnimate(Button button)
{
    // Keine Animation, leer lassen
}

    // Bestehende Animation
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
            RepeatBehavior = new RepeatBehavior(2)  // Animation 2-mal abspielen
        };

        var scaleYAnimation = new DoubleAnimation
        {
            From = 1.0,
            To = 1.2,
            Duration = new Duration(TimeSpan.FromSeconds(0.3)),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(2)  // Animation 2-mal abspielen
        };

        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
    }

    // Neue Animation
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
            RepeatBehavior = new RepeatBehavior(1)  // Animation einmal abspielen
        };

        rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
    }

    // Methode zur Entscheidung, welche Animation verwendet wird
public static void AnimateButtonByChoice(Button button, int choice)
{
    switch (choice)
    {
        case 1:
            AnimateButton(button);
            break;
        case 2:
            AnimateButton2(button);
            break;
        default:
            NotAnimate(button);  // Standardmethode, keine Animation
            break;
    }
}





}
