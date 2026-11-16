using System.Globalization;
using System.Net;
using System.Text.Json;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication.OAuth2;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.YouTube;
public sealed class YouTubeOAuth(InvocationContext context, HttpClient? httpClient = null) : BaseInvocable(context), IOAuth2AuthorizeService, IOAuth2TokenService, ITokenRefreshable
{
    public const string Scope = "https://www.googleapis.com/auth/youtube.force-ssl";
    public const string MonetaryScope = "https://www.googleapis.com/auth/yt-analytics-monetary.readonly";
    public const string AnalyticsScope = "https://www.googleapis.com/auth/yt-analytics.readonly";
    public const string ReadOnlyScope = "https://www.googleapis.com/auth/youtube.readonly";
    public const string RequestedScopes = Scope + " " + AnalyticsScope + " " + ReadOnlyScope;
    private static readonly HttpClient SharedClient = new(new SocketsHttpHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(60)
    };
    private readonly HttpClient client = httpClient ?? SharedClient;
    public string GetAuthorizationUrl(Dictionary<string, string> values)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = Required(values, "client_id"),
            ["redirect_uri"] = InvocationContext.UriInfo.AuthorizationCodeRedirectUri.ToString(),
            ["response_type"] = "code",
            ["scope"] = RequestedScopes + (values.GetValueOrDefault("report_access") == "monetary" ? " " + MonetaryScope : ""),
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["include_granted_scopes"] = "true",
            ["state"] = Required(values, "state")
        };
        return "https://accounts.google.com/o/oauth2/v2/auth?" + string.Join("&", query.Select(p => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value)));
    }

    public bool IsRefreshToken(Dictionary<string, string> values) => !TryExpiry(values, out var expires) || expires <= DateTimeOffset.UtcNow.AddMinutes(2);
    public int? GetRefreshTokenExprireInMinutes(Dictionary<string, string> values) => TryExpiry(values, out var expires) ? Math.Max(0, (int)(expires - DateTimeOffset.UtcNow).TotalMinutes - 2) : 0;
    public Task<Dictionary<string, string>> RequestToken(string state, string code, Dictionary<string, string> values, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(state))
            throw new PluginMisconfigurationException("OAuth state is missing. Start the connection again.");
        if (values.TryGetValue("state", out var storedState) && storedState != state)
            throw new PluginMisconfigurationException("OAuth state does not match. Start the connection again.");
        return ExchangeAsync(values, new() { ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = InvocationContext.UriInfo.AuthorizationCodeRedirectUri.ToString() }, true, cancellationToken);
    }

    public Task<Dictionary<string, string>> RefreshToken(Dictionary<string, string> values, CancellationToken cancellationToken) => ExchangeAsync(values, new() { ["grant_type"] = "refresh_token", ["refresh_token"] = Required(values, "refresh_token") }, false, cancellationToken);
    public async Task RevokeToken(Dictionary<string, string> values)
    {
        var token = values.GetValueOrDefault("refresh_token") ?? values.GetValueOrDefault("access_token");
        if (string.IsNullOrEmpty(token))
            return;
        using var response = await PostAsync("https://oauth2.googleapis.com/revoke", new() { ["token"] = token }, CancellationToken.None);
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.BadRequest)
            throw new PluginApplicationException("Google could not revoke this connection. Retry or remove access in your Google Account permissions.");
    }

    private async Task<Dictionary<string, string>> ExchangeAsync(Dictionary<string, string> values, Dictionary<string, string> form, bool initial, CancellationToken ct)
    {
        form["client_id"] = Required(values, "client_id");
        form["client_secret"] = Required(values, "client_secret");
        using var response = await PostAsync("https://oauth2.googleapis.com/token", form, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new PluginMisconfigurationException("Google rejected the OAuth token request. Check the client ID, client secret and redirect URI, and reconnect if consent has expired or been revoked.");
        JsonElement data;
        try
        {
            using var doc = JsonDocument.Parse(body);
            data = doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            throw new PluginApplicationException("Google returned an invalid OAuth token response. Reconnect the account.");
        }

        var access = YouTubeClient.Text(data, "access_token");
        var refresh = YouTubeClient.Text(data, "refresh_token");
        if (string.IsNullOrEmpty(refresh))
            refresh = values.GetValueOrDefault("refresh_token", "");
        if (string.IsNullOrEmpty(access) || string.IsNullOrEmpty(refresh) || !data.TryGetProperty("expires_in", out var ttl) || !ttl.TryGetInt32(out var seconds) || seconds <= 0)
            throw new PluginMisconfigurationException("Google did not return the required access/refresh tokens. Reconnect and approve offline access; remove the old Google app grant first if needed.");
        var scope = YouTubeClient.Text(data, "scope");
        if (initial && !scope.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(Scope))
            throw new PluginMisconfigurationException("Approve the requested YouTube management permission and reconnect the account.");
        var result = new Dictionary<string, string>(values)
        {
            ["client_id"] = form["client_id"],
            ["client_secret"] = form["client_secret"],
            ["access_token"] = access,
            ["refresh_token"] = refresh,
            ["expires_at"] = DateTimeOffset.UtcNow.AddSeconds(seconds).ToString("O", CultureInfo.InvariantCulture)
        };
        if (!string.IsNullOrEmpty(scope))
            result["scope"] = scope;
        result.Remove("state");
        result.Remove("code");
        return result;
    }

    private async Task<HttpResponseMessage> PostAsync(string url, Dictionary<string, string> form, CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(form);
        try
        {
            return await client.PostAsync(url, content, ct);
        }
        catch (OperationCanceledException)when (!ct.IsCancellationRequested)
        {
            throw new PluginApplicationException("Google OAuth timed out. Retry the connection.");
        }
        catch (HttpRequestException)
        {
            throw new PluginApplicationException("Google OAuth could not be reached. Retry the connection.");
        }
    }

    private static string Required(Dictionary<string, string> values, string key) => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : throw new PluginMisconfigurationException($"OAuth connection is missing {key}. Enter the Google Cloud OAuth client credentials and reconnect.");
    private static bool TryExpiry(Dictionary<string, string> values, out DateTimeOffset expires) => DateTimeOffset.TryParse(values.GetValueOrDefault("expires_at"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out expires);
}
