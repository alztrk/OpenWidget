using System.Linq;
using OpenWidget.Services;
using Xunit;

namespace OpenWidget.Tests;

public class WorldCityServiceTests
{
    public WorldCityServiceTests()
    {
        WorldCityService.SetTurkishDistricts(null);
    }

    [Fact]
    public void GetAll_ReturnsPopulatedCountryList()
    {
        var groups = WorldCityService.GetAll();

        Assert.NotEmpty(groups);
        var turkey = groups.FirstOrDefault(g => g.CountryCode == "TR");
        Assert.NotNull(turkey);
        Assert.Equal(81, turkey.Cities.Count);
        Assert.Contains("İstanbul", turkey.Cities);
    }

    [Fact]
    public void Filter_WithEmptyOrNullQuery_ReturnsAllGroups()
    {
        var all = WorldCityService.GetAll();
        var fromEmpty = WorldCityService.Filter("");
        var fromNull = WorldCityService.Filter(null);

        Assert.Equal(all.Count, fromEmpty.Count);
        Assert.Equal(all.Count, fromNull.Count);
    }

    [Fact]
    public void Filter_TurkishCity_ReturnsMatchingGroupAndCity()
    {
        var results = WorldCityService.Filter("Ankara");

        Assert.NotEmpty(results);
        var tr = results.FirstOrDefault(g => g.CountryCode == "TR");
        Assert.NotNull(tr);
        Assert.Contains("Ankara", tr.Cities);
    }

    [Fact]
    public void Filter_TurkishDiacritics_HandlesCaseInsensitiveMatch()
    {
        var results = WorldCityService.Filter("istanbul");

        Assert.NotEmpty(results);
        var tr = results.FirstOrDefault(g => g.CountryCode == "TR");
        Assert.NotNull(tr);
        Assert.Contains("İstanbul", tr.Cities);
    }

    [Fact]
    public void Filter_TurkishDistrict_ReturnsMatchingEntry()
    {
        WorldCityService.SetTurkishDistricts(["Kadıköy, İstanbul", "Sancaktepe, İstanbul", "Çekmeköy, İstanbul"]);

        var results = WorldCityService.Filter("Sancaktepe");

        Assert.NotEmpty(results);
        var tr = results.FirstOrDefault(g => g.CountryCode == "TR");
        Assert.NotNull(tr);
        Assert.Contains("Sancaktepe, İstanbul", tr.Cities);
        Assert.Contains("İstanbul", WorldCityService.GetAll().First(g => g.CountryCode == "TR").Cities);
    }

    [Fact]
    public void Filter_NonExistentCity_ReturnsEmpty()
    {
        var results = WorldCityService.Filter("NonExistentCityNameXYZ12345");

        Assert.Empty(results);
    }
}
