using System;
using System.IO;
using System.Linq;
using OpenWidget.Services;
using Xunit;

namespace OpenWidget.Tests;

public class MarketFinanceServiceTests
{
    private const string SampleMarketJson = """
        {
          "Update_Date": "2026-11-02 16:00:05",
          "USD": { "Buying": "49,9629", "Type": "Currency", "Selling": "49,9757", "Change": "%0,05" },
          "EUR": { "Buying": "56,0621", "Type": "Currency", "Selling": "56,0704", "Change": "%0,54" },
          "GBP": { "Buying": "67,0813", "Type": "Currency", "Selling": "67,0877", "Change": "%0,16" },
          "CAD": { "Buying": "35,7414", "Type": "Currency", "Selling": "35,7582", "Change": "%0,03" },
          "gram-altin": { "Buying": "6.267,19", "Type": "Gold", "Selling": "6.268,07", "Change": "%0,73" },
          "ceyrek-altin": { "Buying": "10.024,11", "Type": "Gold", "Selling": "10.253,63", "Change": "%0,42" },
          "ons": { "Buying": "$3.901,02", "Type": "Gold", "Selling": "$3.901,57", "Change": "%0,67" }
        }
        """;

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
            Assert.Equal(5, specs.VisibleItems.Count);

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

    [Fact]
    public void CachedMarketJson_ParsesEveryInstrumentAndFiltersVisibleKeys()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"finance_test_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempFile, SampleMarketJson);
            var service = new MarketFinanceService(tempFile);
            service.SetVisibleKeys(["GBP", "gram-altin", "CAD"]);

            var specs = service.GetCurrentSpecs();

            Assert.Equal(4, specs.Currencies.Count);
            Assert.Equal(3, specs.Gold.Count);
            Assert.Equal(3, specs.VisibleItems.Count);
            Assert.Equal(new[] { "GBP", "gram-altin", "CAD" }, specs.VisibleItems.Select(item => item.Key).ToArray());
            Assert.Contains(specs.VisibleItems, item => item.ValueFormatted.Contains("₺"));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void NormalizeVisibleKeys_FallsBackToDefaultsWhenEmpty()
    {
        var keys = FinanceInstrumentCatalog.NormalizeVisibleKeys([]);

        Assert.Equal(FinanceInstrumentCatalog.DefaultVisibleKeys, keys);
    }
}
