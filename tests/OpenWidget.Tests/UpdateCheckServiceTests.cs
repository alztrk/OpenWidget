using OpenWidget.Services;
using Xunit;

namespace OpenWidget.Tests;

public class UpdateCheckServiceTests
{
    [Fact]
    public void CurrentVersion_IsDefinedAndValidFormat()
    {
        Assert.False(string.IsNullOrWhiteSpace(UpdateCheckService.CurrentVersion));
        Assert.True(System.Version.TryParse(UpdateCheckService.CurrentVersion, out var ver));
        Assert.True(ver.Major >= 1);
    }

    [Fact]
    public void UpdateCheckResult_Record_InitializesCorrectly()
    {
        var result = new UpdateCheckResult(
            IsSuccess: true,
            IsUpdateAvailable: true,
            CurrentVersion: "1.2.0",
            LatestVersion: "1.3.0",
            ReleaseNotes: "New features added",
            ReleaseUrl: "https://github.com/alztrk/OpenWidget/releases/v1.3.0"
        );

        Assert.True(result.IsSuccess);
        Assert.True(result.IsUpdateAvailable);
        Assert.Equal("1.2.0", result.CurrentVersion);
        Assert.Equal("1.3.0", result.LatestVersion);
        Assert.Equal("New features added", result.ReleaseNotes);
        Assert.Equal("https://github.com/alztrk/OpenWidget/releases/v1.3.0", result.ReleaseUrl);
    }
}
