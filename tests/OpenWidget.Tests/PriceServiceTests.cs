using System;
using System.IO;
using OpenWidget.Models;
using OpenWidget.Services;
using Xunit;

namespace OpenWidget.Tests;

public class PriceServiceTests
{
    [Fact]
    public void PriceService_FormatsPricesAndTotalAccurately()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"prices_test_{Guid.NewGuid():N}.json");
        try
        {
            var prices = new HardwarePrices
            {
                Currency = "₺",
                LastUpdated = DateTime.Today.ToString("yyyy-MM-dd")
            };
            prices.Items["cpu"] = 6500m;
            prices.Items["gpu"] = 12000m;
            prices.Save(tempFile);

            var service = new PriceService(tempFile);

            Assert.Equal("6.500 ₺", service.GetPriceFormatted("cpu"));
            Assert.Equal("12.000 ₺", service.GetPriceFormatted("gpu"));
            Assert.Equal("- ₺", service.GetPriceFormatted("non_existent"));
            Assert.Equal(6500m, service.GetPriceValue("cpu"));
            Assert.Equal(0m, service.GetPriceValue("non_existent"));
            Assert.Equal("18.500 ₺", service.GetTotalPriceFormatted());
            Assert.Equal("Bugün", service.GetLastUpdatedFormatted());
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
