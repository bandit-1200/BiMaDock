using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

public class ButtonAnimations
{
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
}
