using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace FrosthavenCompanion.App.Services;

/// <summary>
/// Stores the campaign save in a private GitHub gist so it can be shared across
/// the user's own devices. The token and gist id live in the browser's
/// localStorage (per device). Talks to the GitHub REST API directly from the
/// browser — no backend required.
/// </summary>
public sealed class GistSyncService(IJSRuntime js)
{
    private const string TokenKey = "frosthaven.sync.token";
    private const string GistIdKey = "frosthaven.sync.gistId";
    private const string GistFileName = "frosthaven-campaign.json";
    private const string GistDescription = "Frosthaven Companion campaign sync";

    private readonly HttpClient http = new() { BaseAddress = new Uri("https://api.github.com/") };
    private string? token;
    private bool settingsLoaded;

    public string? GistId { get; private set; }
    public bool Connected => !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(GistId);

    /// <summary>Reads any saved token/gist id from localStorage (once).</summary>
    public async Task EnsureLoadedAsync()
    {
        if (settingsLoaded) return;
        token = await js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
        GistId = await js.InvokeAsync<string?>("localStorage.getItem", GistIdKey);
        settingsLoaded = true;
    }

    /// <summary>
    /// Validates the token and locates an existing campaign gist (if any).
    /// Returns true if a gist was found, false if none exists yet. Throws on a
    /// bad token.
    /// </summary>
    public async Task<bool> ConnectAsync(string personalAccessToken)
    {
        token = personalAccessToken.Trim();

        using var request = Authorized(HttpMethod.Get, "gists?per_page=100");
        using var response = await http.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("That token was rejected. Make sure it's a classic token with the 'gist' scope.");
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (var gist in doc.RootElement.EnumerateArray())
        {
            if (gist.TryGetProperty("files", out var files) &&
                files.TryGetProperty(GistFileName, out _))
            {
                GistId = gist.GetProperty("id").GetString();
                await PersistAsync();
                return true;
            }
        }

        await PersistAsync(); // keep the token; gist will be created on first push
        return false;
    }

    /// <summary>Creates the campaign gist with the given content.</summary>
    public async Task CreateAsync(string content)
    {
        using var request = Authorized(HttpMethod.Post, "gists");
        request.Content = JsonContent.Create(new
        {
            description = GistDescription,
            @public = false,
            files = new Dictionary<string, object> { [GistFileName] = new { content } },
        });
        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        GistId = doc.RootElement.GetProperty("id").GetString();
        await PersistAsync();
    }

    /// <summary>Reads the campaign JSON from the gist, or null if not connected.</summary>
    public async Task<string?> PullAsync()
    {
        if (!Connected) return null;

        using var request = Authorized(HttpMethod.Get, $"gists/{GistId}");
        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("files").TryGetProperty(GistFileName, out var file)
            ? file.GetProperty("content").GetString()
            : null;
    }

    /// <summary>Writes the campaign JSON to the gist (creating it if needed).</summary>
    public async Task PushAsync(string content)
    {
        if (string.IsNullOrEmpty(token)) return;
        if (string.IsNullOrEmpty(GistId))
        {
            await CreateAsync(content);
            return;
        }

        using var request = Authorized(HttpMethod.Patch, $"gists/{GistId}");
        request.Content = JsonContent.Create(new
        {
            files = new Dictionary<string, object> { [GistFileName] = new { content } },
        });
        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DisconnectAsync()
    {
        token = null;
        GistId = null;
        await js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await js.InvokeVoidAsync("localStorage.removeItem", GistIdKey);
    }

    private HttpRequestMessage Authorized(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return request;
    }

    private async Task PersistAsync()
    {
        await js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        if (!string.IsNullOrEmpty(GistId))
            await js.InvokeVoidAsync("localStorage.setItem", GistIdKey, GistId);
    }
}
