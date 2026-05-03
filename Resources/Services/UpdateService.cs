using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace CassieWordCheck.Services;

/// <summary>
/// 自动更新——从 GitHub Releases API 检查新版本喵~
/// 比较当前程序集版本和最新 Release 的 tag 版本喵！
/// </summary>
public class UpdateService
{
    // GitHub 仓库信息喵~
    private const string RepoOwner = "qingranawa";
    private const string RepoName = "Cassiewordcheck";
    private const string ApiUrl = "https://api.github.com/repos/" + RepoOwner + "/" + RepoName + "/releases/latest";

    private readonly HttpClient _httpClient = new();
    private readonly Version _currentVersion;

    public UpdateService()
    {
        // 读取程序集版本号喵~
        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        _currentVersion = ver ?? new Version(0, 0, 0);
    }

    /// <summary>当前版本喵~</summary>
    public Version CurrentVersion => _currentVersion;

    /// <summary>异步检查更新，返回更新信息（无更新或失败时返回 null）喵~</summary>
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

            // 去掉 tag 的 "v" 前缀再解析版本号喵~
            var versionStr = tagName.TrimStart('v', 'V');
            if (!Version.TryParse(versionStr, out var latestVersion))
                return null;

            return new UpdateInfo
            {
                LatestVersion = latestVersion,
                TagName = tagName,
                HtmlUrl = htmlUrl,
                ReleaseNotes = body,
                HasUpdate = latestVersion > _currentVersion, // 比较版本喵！
            };
        }
        catch
        {
            return null; // 网络错误就安静返回 null 喵~
        }
    }
}

/// <summary>更新信息喵~</summary>
public class UpdateInfo
{
    public Version LatestVersion { get; init; } = new();
    public string TagName { get; init; } = "";
    public string HtmlUrl { get; init; } = "";
    public string ReleaseNotes { get; init; } = "";
    public bool HasUpdate { get; init; }
}
