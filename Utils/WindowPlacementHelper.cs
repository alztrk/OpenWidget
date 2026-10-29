using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace OpenWidget.Utils;

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

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}

public static class WindowPlacementHelper
{
    public const int WM_MOVING = 0x0216;
    public const uint MONITOR_DEFAULTTONEAREST = 2;

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    public static Rect GetTargetWorkArea(double left, double top)
    {
        var pt = new POINT { X = (int)left + 30, Y = (int)top + 30 };
        var monitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
        if (monitor != IntPtr.Zero)
        {
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
            if (GetMonitorInfo(monitor, ref mi))
            {
                var work = mi.rcWork;
                return new Rect(work.Left, work.Top, work.Right - work.Left, work.Bottom - work.Top);
            }
        }
        return SystemParameters.WorkArea;
    }

    public static Rect GetWindowWorkArea(IntPtr hwnd)
    {
        if (hwnd != IntPtr.Zero)
        {
            var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                if (GetMonitorInfo(monitor, ref mi))
                {
                    var work = mi.rcWork;
                    return new Rect(work.Left, work.Top, work.Right - work.Left, work.Bottom - work.Top);
                }
            }
        }
        return SystemParameters.WorkArea;
    }

    public static void ClampAndSnapMovingRect(ref RECT rect, IntPtr hwnd, int snapDistance = 12)
    {
        var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        if (monitor == IntPtr.Zero) return;

        var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
        if (!GetMonitorInfo(monitor, ref mi)) return;

        var work = mi.rcWork;
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        // Horizontal Snap & Clamp
        if (rect.Left < work.Left || Math.Abs(rect.Left - work.Left) <= snapDistance)
        {
            rect.Left = work.Left;
            rect.Right = work.Left + width;
        }
        else if (rect.Right > work.Right || Math.Abs(rect.Right - work.Right) <= snapDistance)
        {
            rect.Right = work.Right;
            rect.Left = work.Right - width;
        }

        // Vertical Snap & Clamp
        if (rect.Top < work.Top || Math.Abs(rect.Top - work.Top) <= snapDistance)
        {
            rect.Top = work.Top;
            rect.Bottom = work.Top + height;
        }
        else if (rect.Bottom > work.Bottom || Math.Abs(rect.Bottom - work.Bottom) <= snapDistance)
        {
            rect.Bottom = work.Bottom;
            rect.Top = work.Bottom - height;
        }
    }
}
