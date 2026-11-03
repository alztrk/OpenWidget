using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenWidget.Services;

public sealed record FinanceInstrumentDefinition(
    string Key,
    string Group,
    string IconKey,
    string LabelTr,
    string LabelEn);

public static class FinanceInstrumentCatalog
{
    public const string GroupCurrency = "Currency";
    public const string GroupGold = "Gold";
    public const string GroupCrypto = "Crypto";

    public static IReadOnlyList<string> DefaultVisibleKeys { get; } =
    [
        "USD",
        "EUR",
        "gram-altin",
        "btc",
        "eth"
    ];

    public static IReadOnlyList<FinanceInstrumentDefinition> Known { get; } =
    [
        new("USD", GroupCurrency, "IconDollar", "Dolar (USD)", "US Dollar (USD)"),
        new("EUR", GroupCurrency, "IconEuro", "Euro (EUR)", "Euro (EUR)"),
        new("GBP", GroupCurrency, "IconDollar", "Sterlin (GBP)", "Pound (GBP)"),
        new("CHF", GroupCurrency, "IconDollar", "İsviçre Frangı (CHF)", "Swiss Franc (CHF)"),
        new("CAD", GroupCurrency, "IconDollar", "Kanada Doları (CAD)", "Canadian Dollar (CAD)"),
        new("RUB", GroupCurrency, "IconDollar", "Ruble (RUB)", "Ruble (RUB)"),
        new("AED", GroupCurrency, "IconDollar", "Dirhem (AED)", "Dirham (AED)"),
        new("AUD", GroupCurrency, "IconDollar", "Avustralya Doları (AUD)", "Australian Dollar (AUD)"),
        new("DKK", GroupCurrency, "IconDollar", "Danimarka Kronu (DKK)", "Danish Krone (DKK)"),
        new("SEK", GroupCurrency, "IconDollar", "İsveç Kronu (SEK)", "Swedish Krona (SEK)"),
        new("NOK", GroupCurrency, "IconDollar", "Norveç Kronu (NOK)", "Norwegian Krone (NOK)"),
        new("JPY", GroupCurrency, "IconDollar", "Yen (JPY)", "Yen (JPY)"),
        new("KWD", GroupCurrency, "IconDollar", "Kuveyt Dinarı (KWD)", "Kuwaiti Dinar (KWD)"),
        new("ZAR", GroupCurrency, "IconDollar", "Rand (ZAR)", "Rand (ZAR)"),
        new("BHD", GroupCurrency, "IconDollar", "Bahreyn Dinarı (BHD)", "Bahraini Dinar (BHD)"),
        new("LYD", GroupCurrency, "IconDollar", "Libya Dinarı (LYD)", "Libyan Dinar (LYD)"),
        new("SAR", GroupCurrency, "IconDollar", "Suudi Riyali (SAR)", "Saudi Riyal (SAR)"),
        new("IQD", GroupCurrency, "IconDollar", "Irak Dinarı (IQD)", "Iraqi Dinar (IQD)"),
        new("ILS", GroupCurrency, "IconDollar", "Şekel (ILS)", "Shekel (ILS)"),
        new("INR", GroupCurrency, "IconDollar", "Rupi (INR)", "Rupee (INR)"),
        new("MXN", GroupCurrency, "IconDollar", "Peso (MXN)", "Peso (MXN)"),
        new("HUF", GroupCurrency, "IconDollar", "Forint (HUF)", "Forint (HUF)"),
        new("NZD", GroupCurrency, "IconDollar", "Yeni Zelanda Doları (NZD)", "New Zealand Dollar (NZD)"),
        new("BRL", GroupCurrency, "IconDollar", "Real (BRL)", "Real (BRL)"),
        new("IDR", GroupCurrency, "IconDollar", "Rupiah (IDR)", "Rupiah (IDR)"),
        new("CZK", GroupCurrency, "IconDollar", "Koruna (CZK)", "Koruna (CZK)"),
        new("PLN", GroupCurrency, "IconDollar", "Zloti (PLN)", "Zloty (PLN)"),
        new("RON", GroupCurrency, "IconDollar", "Ley (RON)", "Leu (RON)"),
        new("CNY", GroupCurrency, "IconDollar", "Yuan (CNY)", "Yuan (CNY)"),
        new("ARS", GroupCurrency, "IconDollar", "Arjantin Pesosu (ARS)", "Argentine Peso (ARS)"),
        new("ALL", GroupCurrency, "IconDollar", "Lek (ALL)", "Lek (ALL)"),
        new("AZN", GroupCurrency, "IconDollar", "Manat (AZN)", "Manat (AZN)"),
        new("BAM", GroupCurrency, "IconDollar", "Konvertibl Mark (BAM)", "Convertible Mark (BAM)"),
        new("CLP", GroupCurrency, "IconDollar", "Şili Pesosu (CLP)", "Chilean Peso (CLP)"),
        new("COP", GroupCurrency, "IconDollar", "Kolombiya Pesosu (COP)", "Colombian Peso (COP)"),
        new("CRC", GroupCurrency, "IconDollar", "Kolon (CRC)", "Colon (CRC)"),
        new("DZD", GroupCurrency, "IconDollar", "Cezayir Dinarı (DZD)", "Algerian Dinar (DZD)"),
        new("EGP", GroupCurrency, "IconDollar", "Mısır Lirası (EGP)", "Egyptian Pound (EGP)"),
        new("HKD", GroupCurrency, "IconDollar", "Hong Kong Doları (HKD)", "Hong Kong Dollar (HKD)"),
        new("ISK", GroupCurrency, "IconDollar", "İzlanda Kronu (ISK)", "Icelandic Krona (ISK)"),
        new("KRW", GroupCurrency, "IconDollar", "Won (KRW)", "Won (KRW)"),
        new("KZT", GroupCurrency, "IconDollar", "Tenge (KZT)", "Tenge (KZT)"),
        new("LBP", GroupCurrency, "IconDollar", "Lübnan Lirası (LBP)", "Lebanese Pound (LBP)"),
        new("LKR", GroupCurrency, "IconDollar", "Sri Lanka Rupisi (LKR)", "Sri Lankan Rupee (LKR)"),
        new("MAD", GroupCurrency, "IconDollar", "Dirham (MAD)", "Dirham (MAD)"),
        new("MDL", GroupCurrency, "IconDollar", "Leu (MDL)", "Leu (MDL)"),
        new("MKD", GroupCurrency, "IconDollar", "Dinar (MKD)", "Denar (MKD)"),
        new("MYR", GroupCurrency, "IconDollar", "Ringgit (MYR)", "Ringgit (MYR)"),
        new("OMR", GroupCurrency, "IconDollar", "Umman Riyali (OMR)", "Omani Rial (OMR)"),
        new("PEN", GroupCurrency, "IconDollar", "Sol (PEN)", "Sol (PEN)"),
        new("PHP", GroupCurrency, "IconDollar", "Peso (PHP)", "Peso (PHP)"),
        new("PKR", GroupCurrency, "IconDollar", "Pakistan Rupisi (PKR)", "Pakistani Rupee (PKR)"),
        new("QAR", GroupCurrency, "IconDollar", "Katar Riyali (QAR)", "Qatari Riyal (QAR)"),
        new("RSD", GroupCurrency, "IconDollar", "Dinar (RSD)", "Dinar (RSD)"),
        new("SGD", GroupCurrency, "IconDollar", "Singapur Doları (SGD)", "Singapore Dollar (SGD)"),
        new("SYP", GroupCurrency, "IconDollar", "Suriye Lirası (SYP)", "Syrian Pound (SYP)"),
        new("THB", GroupCurrency, "IconDollar", "Baht (THB)", "Baht (THB)"),
        new("TWD", GroupCurrency, "IconDollar", "Yeni Tayvan Doları (TWD)", "New Taiwan Dollar (TWD)"),
        new("UAH", GroupCurrency, "IconDollar", "Grivna (UAH)", "Hryvnia (UAH)"),
        new("UYU", GroupCurrency, "IconDollar", "Uruguay Pesosu (UYU)", "Uruguayan Peso (UYU)"),
        new("GEL", GroupCurrency, "IconDollar", "Lari (GEL)", "Lari (GEL)"),
        new("TND", GroupCurrency, "IconDollar", "Tunus Dinarı (TND)", "Tunisian Dinar (TND)"),
        new("BGN", GroupCurrency, "IconDollar", "Lev (BGN)", "Lev (BGN)"),
        new("VND", GroupCurrency, "IconDollar", "Dong (VND)", "Dong (VND)"),
        new("ons", GroupGold, "IconGoldBar", "Ons Altın", "Gold Ounce"),
        new("gram-altin", GroupGold, "IconGoldBar", "Gram Altın", "Gram Gold"),
        new("gram-has-altin", GroupGold, "IconGoldBar", "Gram Has Altın", "Has Gram Gold"),
        new("ceyrek-altin", GroupGold, "IconGoldQuarter", "Çeyrek Altın", "Quarter Gold"),
        new("yarim-altin", GroupGold, "IconGoldCoin", "Yarım Altın", "Half Gold"),
        new("tam-altin", GroupGold, "IconGoldCoin", "Tam Altın", "Full Gold"),
        new("cumhuriyet-altini", GroupGold, "IconGoldCoin", "Cumhuriyet Altını", "Republic Gold"),
        new("ata-altin", GroupGold, "IconGoldCoin", "Ata Altın", "Ata Gold"),
        new("14-ayar-altin", GroupGold, "IconGoldBar", "14 Ayar Altın", "14K Gold"),
        new("18-ayar-altin", GroupGold, "IconGoldBar", "18 Ayar Altın", "18K Gold"),
        new("22-ayar-bilezik", GroupGold, "IconGoldBar", "22 Ayar Bilezik", "22K Bracelet"),
        new("ikibucuk-altin", GroupGold, "IconGoldCoin", "İkibuçuk Altın", "Two-and-a-half Gold"),
        new("besli-altin", GroupGold, "IconGoldCoin", "Beşli Altın", "Five Gold"),
        new("gremse-altin", GroupGold, "IconGoldCoin", "Gremse Altın", "Gremse Gold"),
        new("resat-altin", GroupGold, "IconGoldCoin", "Reşat Altın", "Resat Gold"),
        new("hamit-altin", GroupGold, "IconGoldCoin", "Hamit Altın", "Hamid Gold"),
        new("gumus", GroupGold, "IconGoldBar", "Gümüş", "Silver"),
        new("gram-platin", GroupGold, "IconGoldBar", "Gram Platin", "Gram Platinum"),
        new("gram-paladyum", GroupGold, "IconGoldBar", "Gram Paladyum", "Gram Palladium"),
        new("btc", GroupCrypto, "IconBitcoin", "Bitcoin (BTC)", "Bitcoin (BTC)"),
        new("eth", GroupCrypto, "IconEthereum", "Ethereum (ETH)", "Ethereum (ETH)")
    ];

    public static FinanceInstrumentDefinition? Find(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return Known.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetLabel(string key)
    {
        var definition = Find(key);
        if (definition == null)
        {
            return key;
        }

        return LocalizationService.CurrentLanguage.Equals("tr", StringComparison.OrdinalIgnoreCase)
            ? definition.LabelTr
            : definition.LabelEn;
    }

    public static string GetIconKey(string key, string? type = null)
    {
        var definition = Find(key);
        if (definition != null)
        {
            return definition.IconKey;
        }

        return string.Equals(type, GroupGold, StringComparison.OrdinalIgnoreCase)
            ? "IconGoldBar"
            : "IconDollar";
    }

    public static string GetGroup(string key, string? type = null)
    {
        var definition = Find(key);
        if (definition != null)
        {
            return definition.Group;
        }

        if (string.Equals(type, GroupGold, StringComparison.OrdinalIgnoreCase))
        {
            return GroupGold;
        }

        if (string.Equals(key, "btc", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, "eth", StringComparison.OrdinalIgnoreCase))
        {
            return GroupCrypto;
        }

        return GroupCurrency;
    }

    public static List<string> NormalizeVisibleKeys(IEnumerable<string>? keys)
    {
        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (keys != null)
        {
            foreach (var key in keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                var trimmed = key.Trim();
                if (seen.Add(trimmed))
                {
                    normalized.Add(trimmed);
                }
            }
        }

        return normalized.Count > 0 ? normalized : DefaultVisibleKeys.ToList();
    }
}
