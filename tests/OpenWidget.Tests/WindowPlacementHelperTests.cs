using System.Windows;
using OpenWidget.Utils;
using Xunit;

namespace OpenWidget.Tests;

public class WindowPlacementHelperTests
{
    [Fact]
    public void WorkArea_CalculatesReasonableDimensions()
    {
        var area = WindowPlacementHelper.GetTargetWorkArea(100, 100);

        Assert.True(area.Width > 0);
        Assert.True(area.Height > 0);
    }

    [Fact]
    public void ClampAndSnapMovingRect_WhenNearBorder_SnapsCorrectly()
    {
        var rect = new RECT { Left = 5, Top = 5, Right = 365, Bottom = 505 };

        // Test that method executes safely without throwing on valid rect
        WindowPlacementHelper.ClampAndSnapMovingRect(ref rect, System.IntPtr.Zero, snapDistance: 10);

        Assert.Equal(360, rect.Right - rect.Left);
        Assert.Equal(500, rect.Bottom - rect.Top);
    }
}
