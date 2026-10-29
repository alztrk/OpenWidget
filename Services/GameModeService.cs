using System;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenWidget.Services;

public static class GameModeService
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
    }

    /// <summary>
    /// Checks if a foreground full-screen application or game covers the active monitor.
    /// Excludes Windows desktop shell, explorer taskbar, and own window.
    /// </summary>
    public static bool IsFullscreenAppRunning(IntPtr ownHwnd)
    {
        try
        {
            var fg = GetForegroundWindow();
            if (fg == IntPtr.Zero || fg == ownHwnd) return false;
            if (IsIconic(fg)) return false;

            var sb = new StringBuilder(256);
            GetClassName(fg, sb, sb.Capacity);
            var className = sb.ToString();

            // Shell, desktop, and system overlays are not games
            if (className is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "Windows.UI.Core.CoreWindow")
            {
                return false;
            }

            if (!GetWindowRect(fg, out var rect)) return false;

            var monitor = MonitorFromWindow(fg, MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero) return false;

            var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
            if (!GetMonitorInfo(monitor, ref mi)) return false;

            var screen = mi.rcMonitor;

            // Check if the foreground window completely covers the monitor screen
            return rect.Left <= screen.Left &&
                   rect.Top <= screen.Top &&
                   rect.Right >= screen.Right &&
                   rect.Bottom >= screen.Bottom;
        }
        catch
        {
            return false;
        }
    }
}
