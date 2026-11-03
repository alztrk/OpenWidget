using OpenWidget.Services;
using Xunit;

namespace OpenWidget.Tests;

public class WeatherLocationFormatterTests
{
    [Fact]
    public void Format_DistrictAndProvince_JoinsWithComma()
    {
        var text = WeatherLocationFormatter.Format("Kadıköy", null, "İstanbul");

        Assert.Equal("Kadıköy, İstanbul", text);
    }

    [Fact]
    public void Format_DuplicateCityAndRegion_DoesNotRepeat()
    {
        var text = WeatherLocationFormatter.Format("İstanbul", null, "İstanbul");

        Assert.Equal("İstanbul", text);
    }

    [Fact]
    public void Format_IpDistrictField_PrefersDistrictOverCity()
    {
        var text = WeatherLocationFormatter.Format("İstanbul", "Kadıköy", "İstanbul");

        Assert.Equal("Kadıköy, İstanbul", text);
    }

    [Fact]
    public void Format_EmptyValues_UsesFallback()
    {
        var text = WeatherLocationFormatter.Format("  ", null, "");

        Assert.Equal("İstanbul", text);
    }
}
