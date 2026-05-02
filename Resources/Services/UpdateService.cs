using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace CassieWordCheck.Services;

public class UpdateService
{
    private const string RepoOwner = "Small-Jeff";
    private const string RepoName = "Cassiewordcheck";
    private const string ApiUrl = "https://api.github.com/repos/" + RepoOwner + "/" + RepoName + "/releases/latest";

    private readonly HttpClient _httpClient = new();
    private readonly Version _currentVersion;

    public UpdateService()
    {
        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        _currentVersion = ver ?? new Version(0, 0, 0);
    }

    public Version CurrentVersion => _currentVersion;

    public async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl);
            request.Headers.UserAgent.ParseAdd(RepoOwner + "/" + RepoName);
            request.Headers.Accept.ParseAdd("application/vnd.github.v3+json");

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString() ?? "";
            var htmlUrl = root.GetProperty("html_url").GetString() ?? "";
            var body = root.TryGetProperty("body", out var b)
                ? b.GetString() ?? ""
                : "";

            var versionStr = tagName.TrimStart('v', 'V');
            if (!Version.TryParse(versionStr, out var latestVersion))
                return null;

            return new UpdateInfo
            {
                LatestVersion = latestVersion,
                TagName = tagName,
                HtmlUrl = htmlUrl,
                ReleaseNotes = body,
                HasUpdate = latestVersion > _currentVersion,
            };
        }
        catch
        {
            return null;
        }
    }
}

public class UpdateInfo
{
    public Version LatestVersion { get; init; } = new();
    public string TagName { get; init; } = "";
    public string HtmlUrl { get; init; } = "";
    public string ReleaseNotes { get; init; } = "";
    public bool HasUpdate { get; init; }
}
