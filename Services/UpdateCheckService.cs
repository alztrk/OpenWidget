using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenWidget.Services;

public record UpdateCheckResult(
    bool IsSuccess,
    bool IsUpdateAvailable,
    string CurrentVersion,
    string LatestVersion,
    string ReleaseNotes,
    string ReleaseUrl,
    string? ErrorMessage = null
);

public static class UpdateCheckService
{
    public const string CurrentVersion = "1.3.9";
    private const string GitHubReleasesApi = "https://api.github.com/repos/alztrk/OpenWidget/releases/latest";
    public const string ProjectUrl = "https://github.com/alztrk/OpenWidget";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(6)
    };

    static UpdateCheckService()
    {
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "OpenWidget-App");
        HttpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    }

    public static async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        try
        {
            var response = await HttpClient.GetAsync(GitHubReleasesApi);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult(
                    IsSuccess: false,
                    IsUpdateAvailable: false,
                    CurrentVersion: CurrentVersion,
                    LatestVersion: CurrentVersion,
                    ReleaseNotes: string.Empty,
                    ReleaseUrl: ProjectUrl,
                    ErrorMessage: $"HTTP {(int)response.StatusCode}"
                );
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.TryGetProperty("tag_name", out var tagElem) ? tagElem.GetString() ?? "" : "";
            var htmlUrl = root.TryGetProperty("html_url", out var urlElem) ? urlElem.GetString() ?? ProjectUrl : ProjectUrl;
            var body = root.TryGetProperty("body", out var bodyElem) ? bodyElem.GetString() ?? "" : "";

            var cleanLatest = tagName.TrimStart('v', 'V').Trim();
            var cleanCurrent = CurrentVersion.TrimStart('v', 'V').Trim();

            bool isNewer = false;
            if (Version.TryParse(cleanLatest, out var latestVer) && Version.TryParse(cleanCurrent, out var currVer))
            {
                isNewer = latestVer > currVer;
            }
            else if (!string.IsNullOrEmpty(cleanLatest) && cleanLatest != cleanCurrent)
            {
                isNewer = string.Compare(cleanLatest, cleanCurrent, StringComparison.OrdinalIgnoreCase) > 0;
            }

            return new UpdateCheckResult(
                IsSuccess: true,
                IsUpdateAvailable: isNewer,
                CurrentVersion: CurrentVersion,
                LatestVersion: string.IsNullOrEmpty(cleanLatest) ? CurrentVersion : cleanLatest,
                ReleaseNotes: body,
                ReleaseUrl: htmlUrl
            );
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(
                IsSuccess: false,
                IsUpdateAvailable: false,
                CurrentVersion: CurrentVersion,
                LatestVersion: CurrentVersion,
                ReleaseNotes: string.Empty,
                ReleaseUrl: ProjectUrl,
                ErrorMessage: ex.Message
            );
        }
    }
}
