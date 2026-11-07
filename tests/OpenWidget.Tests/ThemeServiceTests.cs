using System;
using System.Threading;
using System.Windows;
using OpenWidget.Services;
using Xunit;

namespace OpenWidget.Tests;

public class ThemeServiceTests
{
    private static readonly object AppLock = new();

    [Theory]
    [InlineData(1.0, 11.0, 21.0)]
    [InlineData(0.85, 9.4, 17.9)]
    [InlineData(1.15, 12.7, 24.2)]
    [InlineData(1.3, 14.3, 27.3)]
    public void ApplyTextScale_UpdatesResourceTokens_Correctly(double scale, double expectedBody, double expectedClock)
    {
        RunInSta(() =>
        {
            ThemeService.ApplyTextScale(scale);

            var bodyVal = Application.Current.Resources["FontSizeBody"] as double?;
            var clockVal = Application.Current.Resources["FontSizeClock"] as double?;

            Assert.NotNull(bodyVal);
            Assert.NotNull(clockVal);
            Assert.Equal(expectedBody, bodyVal.Value, precision: 1);
            Assert.Equal(expectedClock, clockVal.Value, precision: 1);
        });
    }

    [Theory]
    [InlineData(0.2, 0.8)] // Under min -> clamped to 0.8
    [InlineData(3.0, 1.5)] // Over max -> clamped to 1.5
    [InlineData(-0.5, 1.0)] // Negative -> default 1.0
    [InlineData(double.NaN, 1.0)] // NaN -> default 1.0
    public void ApplyTextScale_ClampsInvalidOrOutOfRangeValues(double inputScale, double expectedEffectiveScale)
    {
        RunInSta(() =>
        {
            ThemeService.ApplyTextScale(inputScale);

            var bodyVal = Application.Current.Resources["FontSizeBody"] as double?;
            var expectedBody = Math.Round(11.0 * expectedEffectiveScale, 1, MidpointRounding.AwayFromZero);

            Assert.NotNull(bodyVal);
            Assert.Equal(expectedBody, bodyVal.Value, precision: 1);
        });
    }

    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                lock (AppLock)
                {
                    if (Application.Current == null)
                    {
                        try
                        {
                            _ = new Application();
                        }
                        catch (InvalidOperationException)
                        {
                            // Already created in another thread
                        }
                    }
                }

                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (exception != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }
}
