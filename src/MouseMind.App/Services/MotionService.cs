using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MouseMind.App.Services;

public static class MotionService
{
    public static bool IsEnabled => SystemParameters.ClientAreaAnimation;

    private static readonly IEasingFunction EnterEase = new CubicEase
    {
        EasingMode = EasingMode.EaseOut
    };

    public static void Reveal(FrameworkElement element, double distance = 6)
    {
        if (!IsEnabled)
        {
            element.Opacity = 1;
            element.RenderTransform = Transform.Identity;
            return;
        }

        var translate = new TranslateTransform(0, distance);
        element.RenderTransform = translate;
        element.Opacity = 0;
        element.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(190)) { EasingFunction = EnterEase });
        translate.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(distance, 0, TimeSpan.FromMilliseconds(230)) { EasingFunction = EnterEase });
    }

    public static void StartOrbit(FrameworkElement element, double from, double to, double seconds)
    {
        if (!IsEnabled) return;
        var rotate = element.RenderTransform as RotateTransform ?? new RotateTransform();
        element.RenderTransformOrigin = new Point(0.5, 0.5);
        element.RenderTransform = rotate;
        rotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(from, to,
            TimeSpan.FromSeconds(seconds))
        {
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = null
        });
    }

    public static void Pulse(FrameworkElement element)
    {
        if (!IsEnabled) return;
        var scale = element.RenderTransform as ScaleTransform ?? new ScaleTransform(1, 1);
        element.RenderTransformOrigin = new Point(0.5, 0.5);
        element.RenderTransform = scale;

        var frames = new DoubleAnimationUsingKeyFrames();
        frames.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        frames.KeyFrames.Add(new EasingDoubleKeyFrame(1.035, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(80)), EnterEase));
        frames.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(210)), EnterEase));
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, frames);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, frames.Clone());
    }

    public static void ShowToast(FrameworkElement element, Action? completed = null)
    {
        element.Visibility = Visibility.Visible;
        element.BeginAnimation(UIElement.OpacityProperty, null);

        if (!IsEnabled)
        {
            element.Opacity = 1;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2200) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                element.Visibility = Visibility.Collapsed;
                completed?.Invoke();
            };
            timer.Start();
            return;
        }

        var translate = new TranslateTransform(0, 8);
        element.RenderTransform = translate;
        var opacity = new DoubleAnimationUsingKeyFrames();
        opacity.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        opacity.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180)), EnterEase));
        opacity.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(2100))));
        opacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(2260)), EnterEase));
        opacity.Completed += (_, _) =>
        {
            element.Visibility = Visibility.Collapsed;
            completed?.Invoke();
        };
        element.BeginAnimation(UIElement.OpacityProperty, opacity);
        translate.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(200)) { EasingFunction = EnterEase });
    }
}
