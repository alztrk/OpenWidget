using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OpenWidget.Services;

public class CountryCityGroup
{
    public string CountryName { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public List<string> Cities { get; set; } = new();

    public CountryCityGroup CloneWith(List<string> filteredCities)
    {
        return new CountryCityGroup
        {
            CountryName = CountryName,
            CountryCode = CountryCode,
            Cities = filteredCities
        };
    }
}

public static class WorldCityService
{
    private static readonly List<CountryCityGroup> Directory =
    [
        new CountryCityGroup
        {
            CountryName = "Türkiye",
            CountryCode = "TR",
            Cities =
            [
                "Adana", "Adıyaman", "Afyonkarahisar", "Ağrı", "Aksaray", "Amasya", "Ankara", "Antalya",
                "Ardahan", "Artvin", "Aydın", "Balıkesir", "Bartın", "Batman", "Bayburt", "Bilecik",
                "Bingöl", "Bitlis", "Bolu", "Burdur", "Bursa", "Çanakkale", "Çankırı", "Çorum",
                "Denizli", "Diyarbakır", "Düzce", "Edirne", "Elazığ", "Erzincan", "Erzurum", "Eskişehir",
                "Gaziantep", "Giresun", "Gümüşhane", "Hakkari", "Hatay", "Iğdır", "Isparta", "İstanbul",
                "İzmir", "Kahramanmaraş", "Karabük", "Karaman", "Kars", "Kastamonu", "Kayseri", "Kilis",
                "Kırıkkale", "Kırklareli", "Kırşehir", "Kocaeli", "Konya", "Kütahya", "Malatya", "Manisa",
                "Mardin", "Mersin", "Muğla", "Muş", "Nevşehir", "Niğde", "Ordu", "Osmaniye",
                "Rize", "Sakarya", "Samsun", "Şanlıurfa", "Siirt", "Sinop", "Şırnak", "Sivas",
                "Tekirdağ", "Tokat", "Trabzon", "Tunceli", "Uşak", "Van", "Yalova", "Yozgat", "Zonguldak"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Almanya",
            CountryCode = "DE",
            Cities =
            [
                "Berlin", "Münih", "Hamburg", "Köln", "Frankfurt", "Stuttgart", "Düsseldorf",
                "Leipzig", "Dortmund", "Essen", "Bremen", "Dresden", "Hannover", "Nürnberg", "Duisburg"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Birleşik Krallık",
            CountryCode = "GB",
            Cities =
            [
                "Londra", "Manchester", "Birmingham", "Leeds", "Glasgow", "Liverpool",
                "Edinburgh", "Bristol", "Sheffield", "Belfast", "Newcastle", "Cardiff"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Amerika Birleşik Devletleri",
            CountryCode = "US",
            Cities =
            [
                "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia",
                "San Antonio", "San Diego", "Dallas", "San Jose", "Austin", "Seattle",
                "San Francisco", "Miami", "Denver", "Boston", "Washington DC", "Las Vegas", "Atlanta"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Fransa",
            CountryCode = "FR",
            Cities =
            [
                "Paris", "Marsilya", "Lyon", "Toulouse", "Nice", "Nantes",
                "Strasbourg", "Montpellier", "Bordeaux", "Lille", "Rennes", "Reims"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "İtalya",
            CountryCode = "IT",
            Cities =
            [
                "Roma", "Milano", "Napoli", "Torino", "Palermo", "Cenova",
                "Bologna", "Floransa", "Bari", "Katanya", "Venedik", "Verona"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "İspanya",
            CountryCode = "ES",
            Cities =
            [
                "Madrid", "Barselona", "Valensiya", "Sevilla", "Zaragoza", "Malaga",
                "Murcia", "Palma", "Las Palmas", "Bilbao", "Alicante"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Hollanda",
            CountryCode = "NL",
            Cities =
            [
                "Amsterdam", "Rotterdam", "Lahey", "Utrecht", "Eindhoven",
                "Groningen", "Tilburg", "Almere", "Breda", "Nijmegen"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "İsviçre",
            CountryCode = "CH",
            Cities =
            [
                "Zürih", "Cenevre", "Basel", "Lozan", "Bern", "Luzern", "St. Gallen", "Lugano"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Avusturya",
            CountryCode = "AT",
            Cities =
            [
                "Viyana", "Graz", "Linz", "Salzburg", "Innsbruck", "Klagenfurt"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Azerbaycan",
            CountryCode = "AZ",
            Cities =
            [
                "Bakü", "Gence", "Sumgayıt", "Mingeçevir", "Nahçıvan", "Şeki", "Lenkeran"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Japonya",
            CountryCode = "JP",
            Cities =
            [
                "Tokyo", "Yokohama", "Osaka", "Nagoya", "Sapporo", "Fukuoka", "Kobe", "Kyoto", "Hiroshima"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Kanada",
            CountryCode = "CA",
            Cities =
            [
                "Toronto", "Montreal", "Vancouver", "Calgary", "Edmonton", "Ottawa", "Winnipeg", "Quebec City"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Avustralya",
            CountryCode = "AU",
            Cities =
            [
                "Sidney", "Melbourne", "Brisbane", "Perth", "Adelaide", "Gold Coast", "Canberra"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Birleşik Arap Emirlikleri",
            CountryCode = "AE",
            Cities =
            [
                "Dubai", "Abu Dabi", "Şarika", "El Ayn", "Acman", "Resü'l-Hayme"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Rusya",
            CountryCode = "RU",
            Cities =
            [
                "Moskova", "Saint Petersburg", "Novosibirsk", "Yekaterinburg", "Kazan", "Nizhny Novgorod"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Çin",
            CountryCode = "CN",
            Cities =
            [
                "Pekin", "Şanghay", "Guangzhou", "Shenzhen", "Çengdu", "Vuhan", "Hangzhou"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Güney Kore",
            CountryCode = "KR",
            Cities =
            [
                "Seul", "Busan", "İnçon", "Daegu", "Daejeon", "Gwangju", "Ulsan"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Brezilya",
            CountryCode = "BR",
            Cities =
            [
                "Sao Paulo", "Rio de Janeiro", "Brasilia", "Salvador", "Fortaleza", "Belo Horizonte"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "İsveç",
            CountryCode = "SE",
            Cities =
            [
                "Stockholm", "Göteborg", "Malmö", "Uppsala", "Västerås", "Örebro"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Norveç",
            CountryCode = "NO",
            Cities =
            [
                "Oslo", "Bergen", "Trondheim", "Stavanger", "Drammen", "Kristiansand"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Belçika",
            CountryCode = "BE",
            Cities =
            [
                "Brüksel", "Anvers", "Gent", "Charleroi", "Liège", "Brugge"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Yunanistan",
            CountryCode = "GR",
            Cities =
            [
                "Atina", "Selanik", "Patra", "Kandiye", "Larisa", "Volos"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Polonya",
            CountryCode = "PL",
            Cities =
            [
                "Varşova", "Krakow", "Lodz", "Wroclaw", "Poznan", "Gdansk"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Portekiz",
            CountryCode = "PT",
            Cities =
            [
                "Lizbon", "Porto", "Vila Nova de Gaia", "Braga", "Coimbra", "Funchal"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Suudi Arabistan",
            CountryCode = "SA",
            Cities =
            [
                "Riyad", "Cidde", "Mekke", "Medine", "Dammam", "Taif"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Mısır",
            CountryCode = "EG",
            Cities =
            [
                "Kahire", "İskenderiye", "Gize", "Port Said", "Süveyş", "Mansura"
            ]
        },
        new CountryCityGroup
        {
            CountryName = "Arjantin",
            CountryCode = "AR",
            Cities =
            [
                "Buenos Aires", "Cordoba", "Rosario", "Mendoza", "La Plata"
            ]
        }
    ];

    public static List<CountryCityGroup> GetAll() => Directory;

    public static List<CountryCityGroup> Filter(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Directory;
        }

        var cleanQuery = query.Trim();
        var culture = new CultureInfo("tr-TR");
        var result = new List<CountryCityGroup>();

        foreach (var group in Directory)
        {
            var isCountryMatch = culture.CompareInfo.IndexOf(group.CountryName, cleanQuery, CompareOptions.IgnoreCase) >= 0;
            if (isCountryMatch)
            {
                result.Add(group);
                continue;
            }

            var matchingCities = group.Cities
                .Where(city => culture.CompareInfo.IndexOf(city, cleanQuery, CompareOptions.IgnoreCase) >= 0)
                .ToList();

            if (matchingCities.Count > 0)
            {
                result.Add(group.CloneWith(matchingCities));
            }
        }

        return result;
    }
}
