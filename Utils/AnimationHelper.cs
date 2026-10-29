using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OpenWidget.Utils;

public static class AnimationHelper
{
    public static readonly IEasingFunction EaseInOut = new CubicEase { EasingMode = EasingMode.EaseInOut };
    public static readonly IEasingFunction EaseOut = new CubicEase { EasingMode = EasingMode.EaseOut };
    public static readonly IEasingFunction EaseIn = new CubicEase { EasingMode = EasingMode.EaseIn };

    public static void ToggleAccordion(FrameworkElement content, FrameworkElement? arrow, ref bool isExpanded)
    {
        var duration = TimeSpan.FromMilliseconds(220);

        if (content.LayoutTransform is not ScaleTransform scale)
        {
            scale = new ScaleTransform(1, 1);
            content.LayoutTransform = scale;
        }

        if (arrow != null && arrow.RenderTransform is not RotateTransform)
        {
            arrow.RenderTransformOrigin = new Point(0.5, 0.5);
            arrow.RenderTransform = new RotateTransform(0);
        }

        if (isExpanded)
        {
            // Collapse
            var scaleAnim = new DoubleAnimation(1, 0, duration) { EasingFunction = EaseInOut };
            var opacityAnim = new DoubleAnimation(1, 0, duration) { EasingFunction = EaseInOut };

            opacityAnim.Completed += (_, _) =>
            {
                content.Visibility = Visibility.Collapsed;
            };

            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            content.BeginAnimation(UIElement.OpacityProperty, opacityAnim);

            if (arrow?.RenderTransform is RotateTransform rotate)
            {
                var rotateAnim = new DoubleAnimation(0, -90, duration) { EasingFunction = EaseInOut };
                rotate.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
            }

            isExpanded = false;
        }
        else
        {
            // Expand
            content.Visibility = Visibility.Visible;

            var scaleAnim = new DoubleAnimation(0, 1, duration) { EasingFunction = EaseInOut };
            var opacityAnim = new DoubleAnimation(0, 1, duration) { EasingFunction = EaseInOut };

            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            content.BeginAnimation(UIElement.OpacityProperty, opacityAnim);

            if (arrow?.RenderTransform is RotateTransform rotate)
            {
                var rotateAnim = new DoubleAnimation(-90, 0, duration) { EasingFunction = EaseInOut };
                rotate.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
            }

            isExpanded = true;
        }
    }

    public static void AnimateWindowEntrance(Window window, FrameworkElement container, double targetOpacity = 1.0)
    {
        if (container.RenderTransform is not TranslateTransform translate)
        {
            translate = new TranslateTransform(0, -10);
            container.RenderTransform = translate;
        }
        else
        {
            translate.Y = -10;
        }

        container.Opacity = 0;

        var duration = TimeSpan.FromMilliseconds(220);
        var opacityAnim = new DoubleAnimation(0, targetOpacity, duration) { EasingFunction = EaseOut };
        var slideAnim = new DoubleAnimation(-10, 0, duration) { EasingFunction = EaseOut };

        container.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
        translate.BeginAnimation(TranslateTransform.YProperty, slideAnim);
    }

    public static void AnimateWindowExit(Window window, FrameworkElement container, Action? onCompleted = null)
    {
        if (container.RenderTransform is not TranslateTransform translate)
        {
            translate = new TranslateTransform(0, 0);
            container.RenderTransform = translate;
        }

        var duration = TimeSpan.FromMilliseconds(180);
        var opacityAnim = new DoubleAnimation(container.Opacity, 0, duration) { EasingFunction = EaseIn };
        var slideAnim = new DoubleAnimation(0, 8, duration) { EasingFunction = EaseIn };

        opacityAnim.Completed += (_, _) =>
        {
            onCompleted?.Invoke();
            window.Close();
        };

        container.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
        translate.BeginAnimation(TranslateTransform.YProperty, slideAnim);
    }
}
