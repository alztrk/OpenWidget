using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace OpenWidget.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Stream _iconStream;
    private readonly Icon _icon;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly ToolStripMenuItem _toggleWidgetItem;
    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _exitItem;
    private readonly Func<bool> _isWidgetVisible;
    private readonly Action _showWidget;
    private readonly Action _toggleWidget;
    private string _showWidgetText = string.Empty;
    private string _hideWidgetText = string.Empty;
    private bool _disposed;

    public TrayIconService(
        Stream iconStream,
        Func<bool> isWidgetVisible,
        Action showWidget,
        Action toggleWidget,
        Action openSettings,
        Action exit)
    {
        _iconStream = iconStream;
        _icon = new Icon(iconStream);
        _isWidgetVisible = isWidgetVisible;
        _showWidget = showWidget;
        _toggleWidget = toggleWidget;

        _toggleWidgetItem = new ToolStripMenuItem();
        _toggleWidgetItem.Click += (_, _) => _toggleWidget();

        _settingsItem = new ToolStripMenuItem();
        _settingsItem.Click += (_, _) => openSettings();

        _exitItem = new ToolStripMenuItem();
        _exitItem.Click += (_, _) => exit();

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.AddRange(
        [
            _toggleWidgetItem,
            _settingsItem,
            new ToolStripSeparator(),
            _exitItem
        ]);
        _contextMenu.Opening += (_, _) => UpdateToggleWidgetText();

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _contextMenu,
            Icon = _icon,
            Text = "OpenWidget",
            Visible = true
        };
        _notifyIcon.MouseClick += NotifyIcon_MouseClick;
    }

    public void UpdateLabels(string showWidget, string hideWidget, string settings, string exit)
    {
        _showWidgetText = showWidget;
        _hideWidgetText = hideWidget;
        _settingsItem.Text = settings;
        _exitItem.Text = exit;
        UpdateToggleWidgetText();
    }

    private void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _showWidget();
        }
    }

    private void UpdateToggleWidgetText()
    {
        _toggleWidgetItem.Text = _isWidgetVisible() ? _hideWidgetText : _showWidgetText;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        _icon.Dispose();
        _iconStream.Dispose();
        _disposed = true;
    }
}
