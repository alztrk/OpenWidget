using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenWidget.Windows;

public sealed class ModularWidgetWindow : Window
{
    private readonly Border _surface;
    public ModularWidgetWindow(string title, UIElement content, ContextMenu? contextMenu, bool isLocked, bool alwaysOnTop)
    {
        Title = title;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ShowActivated = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        Topmost = alwaysOnTop;
        FontFamily = new FontFamily("Roboto, Segoe UI Variable Text, Segoe UI, sans-serif");
        _surface = new Border
        {
            Background = (Brush)Application.Current.FindResource("MasterHudBgBrush"),
            BorderBrush = (Brush)Application.Current.FindResource("MasterHudBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(12, 10, 12, 8),
            Child = content
        };

        Content = _surface;
        ContextMenu = contextMenu;
        PreviewMouseLeftButtonDown += Surface_PreviewMouseLeftButtonDown;
    }

    public void UpdateWindowState(bool isLocked, bool alwaysOnTop, double opacity)
    {
        Topmost = alwaysOnTop;
        Opacity = opacity;
        Cursor = isLocked ? Cursors.Arrow : Cursors.SizeAll;
    }

    private void Surface_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Button || e.OriginalSource is TextBox || e.OriginalSource is ComboBox)
        {
            return;
        }

        if (Cursor == Cursors.Arrow)
        {
            return;
        }

        try
        {
            DragMove();
            e.Handled = true;
        }
        catch (InvalidOperationException)
        {
            // The window can be closed while a mouse message is being dispatched.
        }
    }
}
