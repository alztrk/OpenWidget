using System;
using System.IO;
using OpenWidget.Models;
using Xunit;

namespace OpenWidget.Tests;

public class HardwareConfigTests
{
    [Fact]
    public void DefaultConfig_HasExpectedDefaults()
    {
        var config = new HardwareConfig();

        Assert.NotNull(config.Widget);
        Assert.NotNull(config.Widgets);
        Assert.NotNull(config.CoolingAndPower);
        Assert.NotNull(config.Peripherals);
        Assert.NotNull(config.Accessories);
        Assert.NotNull(config.TelemetryWidget);
        Assert.NotNull(config.FinanceWidget);

        Assert.Equal("tr", config.Widget.Language);
        Assert.Equal("auto", config.Widget.City);
        Assert.Equal("Sky", config.Widget.AccentTheme);
        Assert.True(config.Widgets.ShowHeader);
        Assert.True(config.Widgets.ShowWeather);
        Assert.True(config.Widgets.ShowClipboard);
        Assert.True(config.Widgets.ShowTelemetry);
    }

    [Fact]
    public void Load_NonExistentFile_ReturnsDefaultConfig()
    {
        var fakePath = Path.Combine(Path.GetTempPath(), $"openwidget_test_{Guid.NewGuid():N}.json");

        var config = HardwareConfig.Load(fakePath);

        Assert.NotNull(config);
        Assert.Equal("tr", config.Widget.Language);
    }

    [Fact]
    public void Load_CorruptJson_ReturnsDefaultConfigWithoutThrowing()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"openwidget_corrupt_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempFile, "{ this is not valid json : [ }");

            var config = HardwareConfig.Load(tempFile);

            Assert.NotNull(config);
            Assert.Equal("tr", config.Widget.Language);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_PreservesValues()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"openwidget_roundtrip_{Guid.NewGuid():N}.json");
        try
        {
            var original = new HardwareConfig();
            original.Widget.Language = "en";
            original.Widget.City = "London";
            original.Widget.AccentTheme = "Emerald";
            original.Widget.Opacity = 0.85;
            original.Widget.IsLocked = true;
            original.Widgets.ShowFinance = false;

            original.Save(tempFile);

            Assert.True(File.Exists(tempFile));

            var loaded = HardwareConfig.Load(tempFile);

            Assert.Equal("en", loaded.Widget.Language);
            Assert.Equal("London", loaded.Widget.City);
            Assert.Equal("Emerald", loaded.Widget.AccentTheme);
            Assert.Equal(0.85, loaded.Widget.Opacity);
            Assert.True(loaded.Widget.IsLocked);
            Assert.False(loaded.Widgets.ShowFinance);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Save_SynchronizesLegacyTelemetryAndFinanceFlags()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"openwidget_sync_{Guid.NewGuid():N}.json");
        try
        {
            var config = new HardwareConfig();
            config.Widgets.ShowTelemetry = false;
            config.Widgets.ShowFinance = true;

            config.Save(tempFile);

            Assert.False(config.TelemetryWidget.Enabled);
            Assert.True(config.FinanceWidget.Enabled);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
