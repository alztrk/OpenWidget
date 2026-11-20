using System;
using System.Globalization;
using OpenWidget.Models;

namespace OpenWidget.Services;

public enum FinanceConvertMode
{
    None,
    Spend,
    Quantity
}

public static class FinanceAmountConverter
{
    private static readonly CultureInfo DisplayCulture = new("tr-TR");

    public static bool TryParseAmount(string? text, out decimal amount)
    {
        amount = 0m;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim().Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount)
               && amount >= 0m;
    }

    public static bool IsValidAmountText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return true;
        }

        var separatorSeen = false;
        foreach (var ch in text)
        {
            if (char.IsDigit(ch))
            {
                continue;
            }

            if ((ch is ',' or '.') && !separatorSeen)
            {
                separatorSeen = true;
                continue;
            }

            return false;
        }

        return true;
    }

    public static string FormatDisplayPrice(
        FinanceItem item,
        FinanceConvertMode mode,
        decimal? amount,
        string marketCurrency,
        string? sourceUnitKey = null,
        decimal? sourceUnitPrice = null)
    {
        if (mode == FinanceConvertMode.None || amount is null)
        {
            return item.ValueFormatted;
        }

        if (item.UnitPrice is null or <= 0m)
        {
            return "--";
        }

        return mode switch
        {
            FinanceConvertMode.Spend => FormatSpend(
                amount.Value,
                item.UnitPrice.Value,
                item.Key,
                marketCurrency,
                sourceUnitKey,
                sourceUnitPrice),
            FinanceConvertMode.Quantity => FormatQuantity(amount.Value, item.UnitPrice.Value, marketCurrency),
            _ => item.ValueFormatted
        };
    }

    public static bool TryResolveSourceUnitPrice(
        string? sourceUnitKey,
        string marketCurrency,
        Func<string, decimal?> lookupUnitPrice,
        out decimal sourceUnitPrice)
    {
        sourceUnitPrice = 1m;
        if (string.IsNullOrWhiteSpace(sourceUnitKey)
            || string.Equals(sourceUnitKey, marketCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var price = lookupUnitPrice(sourceUnitKey);
        if (price is null or <= 0m)
        {
            return false;
        }

        sourceUnitPrice = price.Value;
        return true;
    }

    public static string GetUnitOptionCode(string key)
    {
        var group = FinanceInstrumentCatalog.GetGroup(key);
        if (group is FinanceInstrumentCatalog.GroupCurrency or FinanceInstrumentCatalog.GroupCrypto)
        {
            return key.ToUpperInvariant();
        }

        return FinanceInstrumentCatalog.GetLabel(key);
    }

    public static string GetInstrumentUnit(string key)
    {
        var group = FinanceInstrumentCatalog.GetGroup(key);
        if (group == FinanceInstrumentCatalog.GroupCrypto)
        {
            return key.ToUpperInvariant();
        }

        if (group == FinanceInstrumentCatalog.GroupGold)
        {
            if (string.Equals(key, "ons", StringComparison.OrdinalIgnoreCase))
            {
                return "oz";
            }

            if (key.Contains("gram", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "gumus", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("platin", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("paladyum", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("ayar", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("bilezik", StringComparison.OrdinalIgnoreCase))
            {
                return "gr";
            }

            return "adet";
        }

        return key.ToUpperInvariant();
    }

    public static string GetCurrencySymbol(string currency) => currency switch
    {
        "TRY" => "₺",
        "USD" => "$",
        "EUR" => "€",
        "GBP" => "£",
        "JPY" => "¥",
        "CHF" => "CHF",
        "CAD" => "CA$",
        "AUD" => "A$",
        _ => currency
    };

    public static string GetMarketCurrencyLabel(string currency)
    {
        var code = string.IsNullOrWhiteSpace(currency) ? "TRY" : currency.ToUpperInvariant();
        var isTr = LocalizationService.CurrentLanguage.Equals("tr", StringComparison.OrdinalIgnoreCase);
        var name = code switch
        {
            "TRY" => isTr ? "Türk Lirası" : "Turkish Lira",
            "USD" => isTr ? "Dolar" : "US Dollar",
            "EUR" => isTr ? "Euro" : "Euro",
            "GBP" => isTr ? "Sterlin" : "Pound",
            "JPY" => isTr ? "Yen" : "Yen",
            "CHF" => isTr ? "İsviçre Frangı" : "Swiss Franc",
            "CAD" => isTr ? "Kanada Doları" : "Canadian Dollar",
            "AUD" => isTr ? "Avustralya Doları" : "Australian Dollar",
            _ => code
        };

        return $"{name} ({code})";
    }

    public static bool IsInverseSourceRow(
        string itemKey,
        FinanceConvertMode mode,
        decimal? amount,
        string marketCurrency,
        string? sourceUnitKey)
    {
        if (mode != FinanceConvertMode.Spend || amount is null || string.IsNullOrWhiteSpace(sourceUnitKey))
        {
            return false;
        }

        if (string.Equals(sourceUnitKey, marketCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(sourceUnitKey, itemKey, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatSpend(
        decimal amount,
        decimal itemUnitPrice,
        string itemKey,
        string marketCurrency,
        string? sourceUnitKey,
        decimal? sourceUnitPrice)
    {
        decimal sourcePrice = 1m;
        var sourceIsInstrument = !string.IsNullOrWhiteSpace(sourceUnitKey)
            && !string.Equals(sourceUnitKey, marketCurrency, StringComparison.OrdinalIgnoreCase);

        if (sourceIsInstrument)
        {
            if (sourceUnitPrice is null or <= 0m)
            {
                return "--";
            }

            sourcePrice = sourceUnitPrice.Value;

            // Selected unit row → show market-currency value (inverse).
            if (string.Equals(sourceUnitKey, itemKey, StringComparison.OrdinalIgnoreCase))
            {
                return FormatQuantity(amount, sourcePrice, marketCurrency);
            }
        }

        var marketValue = amount * sourcePrice;
        var quantity = marketValue / itemUnitPrice;
        var unit = GetInstrumentUnit(itemKey);
        var group = FinanceInstrumentCatalog.GetGroup(itemKey);
        var decimals = group == FinanceInstrumentCatalog.GroupCrypto ? 6 : 2;
        return $"{quantity.ToString($"N{decimals}", DisplayCulture)} {unit}";
    }

    private static string FormatQuantity(decimal amount, decimal unitPrice, string marketCurrency)
    {
        var total = amount * unitPrice;
        var decimals = string.Equals(marketCurrency, "JPY", StringComparison.OrdinalIgnoreCase) ? 0 : 2;
        return $"{total.ToString($"N{decimals}", DisplayCulture)} {GetCurrencySymbol(marketCurrency)}";
    }
}
