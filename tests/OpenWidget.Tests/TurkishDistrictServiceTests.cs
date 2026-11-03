using OpenWidget.Services;
using Xunit;

namespace OpenWidget.Tests;

public class TurkishDistrictServiceTests
{
    [Fact]
    public void ParseDistrictLocations_JoinsDistrictWithProvinceName()
    {
        const string provincesJson = """
            {"data":[{"id":1,"name":"Adana"},{"id":6,"name":"Ankara"},{"id":7,"name":"Antalya"},{"id":34,"name":"İstanbul"},{"id":35,"name":"İzmir"},{"id":41,"name":"Kocaeli"}]}
            """;
        const string districtsJson = """
            {"data":[
              {"name":"Seyhan","provinceId":1},
              {"name":"Çankaya","provinceId":6},
              {"name":"Muratpaşa","provinceId":7},
              {"name":"Sancaktepe","provinceId":34},
              {"name":"Çekmeköy","provinceId":34},
              {"name":"Sultanbeyli","provinceId":34},
              {"name":"Konak","provinceId":35},
              {"name":"İzmit","provinceId":41}
            ]}
            """;

        var locations = TurkishDistrictService.ParseDistrictLocations(provincesJson, districtsJson);

        Assert.Contains("Sancaktepe, İstanbul", locations);
        Assert.Contains("Çekmeköy, İstanbul", locations);
        Assert.Contains("Sultanbeyli, İstanbul", locations);
        Assert.Contains("Seyhan, Adana", locations);
        Assert.Contains("Çankaya, Ankara", locations);
        Assert.Contains("Muratpaşa, Antalya", locations);
        Assert.Contains("Konak, İzmir", locations);
        Assert.Contains("İzmit, Kocaeli", locations);
    }
}
