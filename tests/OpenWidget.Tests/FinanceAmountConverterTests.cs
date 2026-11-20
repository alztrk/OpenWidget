using OpenWidget.Models;
using OpenWidget.Services;
using Xunit;

namespace OpenWidget.Tests;

public class FinanceAmountConverterTests
{
    [Fact]
    public void Spend_DividesAmountByUnitPrice()
    {
        var item = new FinanceItem("USD", "IconDollar", "Dolar", "49,98 ₺", "%0,05", "Up", UnitPrice: 49.9757m);

        var result = FinanceAmountConverter.FormatDisplayPrice(item, FinanceConvertMode.Spend, 10000m, "TRY");

        Assert.Equal("200,10 USD", result);
    }

    [Fact]
    public void Quantity_MultipliesAmountByUnitPrice()
    {
        var item = new FinanceItem("USD", "IconDollar", "Dolar", "49,98 ₺", "%0,05", "Up", UnitPrice: 49.9757m);

        var result = FinanceAmountConverter.FormatDisplayPrice(item, FinanceConvertMode.Quantity, 2m, "TRY");

        Assert.Equal("99,95 ₺", result);
    }

    [Fact]
    public void EmptyAmount_ReturnsLivePrice()
    {
        var item = new FinanceItem("USD", "IconDollar", "Dolar", "49,98 ₺", "%0,05", "Up", UnitPrice: 49.9757m);

        var result = FinanceAmountConverter.FormatDisplayPrice(item, FinanceConvertMode.Spend, null, "TRY");

        Assert.Equal("49,98 ₺", result);
    }

    [Fact]
    public void ZeroOrMissingUnitPrice_ReturnsDash()
    {
        var missing = new FinanceItem("USD", "IconDollar", "Dolar", "-- ₺", "%0,00", "Neutral");
        var zero = missing with { UnitPrice = 0m };

        Assert.Equal("--", FinanceAmountConverter.FormatDisplayPrice(missing, FinanceConvertMode.Spend, 100m, "TRY"));
        Assert.Equal("--", FinanceAmountConverter.FormatDisplayPrice(zero, FinanceConvertMode.Quantity, 2m, "TRY"));
    }

    [Fact]
    public void CryptoSpend_UsesSixDecimals()
    {
        var item = new FinanceItem("btc", "IconBitcoin", "Bitcoin", "3.200.000 ₺", "%1,00", "Up", UnitPrice: 3_200_000m);

        var result = FinanceAmountConverter.FormatDisplayPrice(item, FinanceConvertMode.Spend, 10000m, "TRY");

        Assert.Equal("0,003125 BTC", result);
    }

    [Fact]
    public void TryParseAmount_AcceptsCommaAndDot()
    {
        Assert.True(FinanceAmountConverter.TryParseAmount("10,5", out var comma));
        Assert.Equal(10.5m, comma);
        Assert.True(FinanceAmountConverter.TryParseAmount("10.5", out var dot));
        Assert.Equal(10.5m, dot);
        Assert.False(FinanceAmountConverter.TryParseAmount("", out _));
        Assert.False(FinanceAmountConverter.TryParseAmount("abc", out _));
    }

    [Fact]
    public void IsValidAmountText_AllowsSingleSeparator()
    {
        Assert.True(FinanceAmountConverter.IsValidAmountText(""));
        Assert.True(FinanceAmountConverter.IsValidAmountText("12"));
        Assert.True(FinanceAmountConverter.IsValidAmountText("12,5"));
        Assert.True(FinanceAmountConverter.IsValidAmountText("12.5"));
        Assert.False(FinanceAmountConverter.IsValidAmountText("12,5.6"));
        Assert.False(FinanceAmountConverter.IsValidAmountText("12a"));
    }

    [Fact]
    public void CachedMarketJson_PopulatesUnitPrice()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"finance_unit_{System.Guid.NewGuid():N}.json");
        try
        {
            System.IO.File.WriteAllText(tempFile, """
                {
                  "Update_Date": "2026-11-02 16:00:05",
                  "USD": { "Buying": "49,9629", "Type": "Currency", "Selling": "49,9757", "Change": "%0,05" },
                  "gram-altin": { "Buying": "6.267,19", "Type": "Gold", "Selling": "6.268,07", "Change": "%0,73" }
                }
                """);

            var service = new MarketFinanceService(tempFile);
            var usd = service.GetCurrentSpecs().Currencies.Find(item => item.Key == "USD");

            Assert.NotNull(usd);
            Assert.Equal(49.9757m, usd!.UnitPrice);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile);
        }
    }

    [Fact]
    public void SpendFromUsd_ConvertsAcrossInstruments()
    {
        var eur = new FinanceItem("EUR", "IconEuro", "Euro", "56,07 ₺", "%0,54", "Up", UnitPrice: 56.0704m);
        var gold = new FinanceItem("gram-altin", "IconGoldBar", "Gram Altın", "6.268,07 ₺", "%0,73", "Up", UnitPrice: 6268.07m);

        // 100 USD at 49.9757 TRY → 4997.57 TRY market value
        var eurResult = FinanceAmountConverter.FormatDisplayPrice(
            eur, FinanceConvertMode.Spend, 100m, "TRY", "USD", 49.9757m);
        var goldResult = FinanceAmountConverter.FormatDisplayPrice(
            gold, FinanceConvertMode.Spend, 100m, "TRY", "USD", 49.9757m);

        Assert.Equal("89,13 EUR", eurResult);
        Assert.Equal("0,80 gr", goldResult);
    }

    [Fact]
    public void SpendFromUsd_SourceRowShowsMarketCurrencyInverse()
    {
        var usd = new FinanceItem("USD", "IconDollar", "Dolar", "49,98 ₺", "%0,05", "Up", UnitPrice: 49.9757m);

        var result = FinanceAmountConverter.FormatDisplayPrice(
            usd, FinanceConvertMode.Spend, 100m, "TRY", "USD", 49.9757m);

        Assert.Equal("4.997,57 ₺", result);
    }

    [Fact]
    public void SpendFromMarketCurrency_MatchesLegacyBehavior()
    {
        var item = new FinanceItem("USD", "IconDollar", "Dolar", "49,98 ₺", "%0,05", "Up", UnitPrice: 49.9757m);

        var withExplicitTry = FinanceAmountConverter.FormatDisplayPrice(
            item, FinanceConvertMode.Spend, 10000m, "TRY", "TRY", 1m);
        var withDefault = FinanceAmountConverter.FormatDisplayPrice(
            item, FinanceConvertMode.Spend, 10000m, "TRY");

        Assert.Equal("200,10 USD", withExplicitTry);
        Assert.Equal("200,10 USD", withDefault);
    }

    [Fact]
    public void TryResolveSourceUnitPrice_UsesOneForMarketCurrency()
    {
        Assert.True(FinanceAmountConverter.TryResolveSourceUnitPrice(
            "TRY", "TRY", _ => 49.9757m, out var price));
        Assert.Equal(1m, price);

        Assert.True(FinanceAmountConverter.TryResolveSourceUnitPrice(
            "USD", "TRY", key => key == "USD" ? 49.9757m : null, out var usd));
        Assert.Equal(49.9757m, usd);

        Assert.False(FinanceAmountConverter.TryResolveSourceUnitPrice(
            "USD", "TRY", _ => null, out _));
    }

    [Fact]
    public void GetUnitOptionCode_UppercasesCurrencyAndCrypto()
    {
        Assert.Equal("USD", FinanceAmountConverter.GetUnitOptionCode("USD"));
        Assert.Equal("BTC", FinanceAmountConverter.GetUnitOptionCode("btc"));
    }

    [Fact]
    public void GetUnitOptionCode_UsesCatalogLabelForGold()
    {
        var previous = LocalizationService.CurrentLanguage;
        try
        {
            LocalizationService.CurrentLanguage = "tr";
            Assert.Equal("Gram Altın", FinanceAmountConverter.GetUnitOptionCode("gram-altin"));
            Assert.Equal("Ata Altın", FinanceAmountConverter.GetUnitOptionCode("ata-altin"));

            LocalizationService.CurrentLanguage = "en";
            Assert.Equal("Gram Gold", FinanceAmountConverter.GetUnitOptionCode("gram-altin"));
            Assert.Equal("Ata Gold", FinanceAmountConverter.GetUnitOptionCode("ata-altin"));
        }
        finally
        {
            LocalizationService.CurrentLanguage = previous;
        }
    }

    [Fact]
    public void GetMarketCurrencyLabel_ReturnsLocalizedName()
    {
        var previous = LocalizationService.CurrentLanguage;
        try
        {
            LocalizationService.CurrentLanguage = "tr";
            Assert.Equal("Türk Lirası (TRY)", FinanceAmountConverter.GetMarketCurrencyLabel("TRY"));

            LocalizationService.CurrentLanguage = "en";
            Assert.Equal("Turkish Lira (TRY)", FinanceAmountConverter.GetMarketCurrencyLabel("TRY"));
            Assert.Equal("US Dollar (USD)", FinanceAmountConverter.GetMarketCurrencyLabel("USD"));
        }
        finally
        {
            LocalizationService.CurrentLanguage = previous;
        }
    }

    [Fact]
    public void IsInverseSourceRow_OnlyWhenSourceInstrumentMatchesRow()
    {
        Assert.True(FinanceAmountConverter.IsInverseSourceRow(
            "USD", FinanceConvertMode.Spend, 100m, "TRY", "USD"));
        Assert.False(FinanceAmountConverter.IsInverseSourceRow(
            "EUR", FinanceConvertMode.Spend, 100m, "TRY", "USD"));
        Assert.False(FinanceAmountConverter.IsInverseSourceRow(
            "USD", FinanceConvertMode.Spend, 100m, "TRY", "TRY"));
        Assert.False(FinanceAmountConverter.IsInverseSourceRow(
            "USD", FinanceConvertMode.None, 100m, "TRY", "USD"));
        Assert.False(FinanceAmountConverter.IsInverseSourceRow(
            "USD", FinanceConvertMode.Spend, null, "TRY", "USD"));
    }
}
