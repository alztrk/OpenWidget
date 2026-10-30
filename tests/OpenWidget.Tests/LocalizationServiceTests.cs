using System.Linq;
using OpenWidget.Services;
using Xunit;

namespace OpenWidget.Tests;

public class LocalizationServiceTests
{
    [Fact]
    public void SupportedLanguages_ContainsAllConfiguredLocales()
    {
        var codes = LocalizationService.SupportedLanguages.Select(language => language.Code).ToArray();

        Assert.Equal(new[] { "tr", "en", "de", "fr", "es", "it", "pt", "ja", "ru" }, codes);
    }

    [Theory]
    [InlineData("tr")]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    [InlineData("pt")]
    [InlineData("ja")]
    [InlineData("ru")]
    public void LanguageSelection_ReturnsLocalizedSettingsTitle(string language)
    {
        LocalizationService.CurrentLanguage = language;

        Assert.NotEqual("SettingsTitle", LocalizationService.Get("SettingsTitle"));
    }

    [Fact]
    public void UnsupportedLanguage_FallsBackToEnglish()
    {
        LocalizationService.CurrentLanguage = "xx";

        Assert.Equal("en", LocalizationService.CurrentLanguage);
        Assert.Equal("Settings", LocalizationService.Get("MenuSettings"));
    }
}
