using System;
using System.IO;
using OpenWidget.Services;
using Xunit;

namespace OpenWidget.Tests;

public class MarketFinanceServiceTests
{
    [Fact]
    public void DefaultFinanceSpecs_DoesNotFabricateFakePrices()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"finance_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new MarketFinanceService(tempFile);
            var specs = service.GetCurrentSpecs();

            Assert.NotNull(specs);
            Assert.NotEmpty(specs.Currencies);
            Assert.NotEmpty(specs.Crypto);

            // Verify clean placeholders instead of fake numbers
            foreach (var curr in specs.Currencies)
            {
                Assert.Equal("-- ₺", curr.ValueFormatted);
            }
            foreach (var crypto in specs.Crypto)
            {
                Assert.Equal("$--", crypto.ValueFormatted);
            }
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
