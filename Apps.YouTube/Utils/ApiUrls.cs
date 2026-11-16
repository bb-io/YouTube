using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.YouTube;
public static class ApiUrls
{
    public static string Build(string resource, Dictionary<string, string> parameters, string? apiKey = null)
    {
        var address = resource.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? resource : resource.StartsWith("upload/", StringComparison.Ordinal) ? "https://www.googleapis.com/" + resource : "https://www.googleapis.com/youtube/v3/" + resource;
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !uri.IsDefaultPort || uri.UserInfo != "" || uri.Fragment != "" || address.Contains('\\') || address.Contains("/../", StringComparison.Ordinal) || address.Contains("/./", StringComparison.Ordinal) || (uri.Host != "www.googleapis.com" && uri.Host != "youtubeanalytics.googleapis.com"))
            throw new PluginMisconfigurationException("Use an HTTPS YouTube Data/Upload API or YouTube Analytics API endpoint. Other hosts and redirects are not supported.");
        var path = Uri.UnescapeDataString(uri.AbsolutePath);
        if (path.Contains("..", StringComparison.Ordinal) || path.Contains('\\') || path.Contains('%') || (uri.Host == "www.googleapis.com" && !path.StartsWith("/youtube/v3/", StringComparison.Ordinal) && !path.StartsWith("/upload/youtube/v3/", StringComparison.Ordinal)) || (uri.Host == "youtubeanalytics.googleapis.com" && path != "/v2/reports"))
            throw new PluginMisconfigurationException("The endpoint must belong to YouTube Data API v3, its upload API, or YouTube Analytics reports.");
        var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            RejectCredential(key);
            query[key] = parts.Length == 2 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : "";
        }

        foreach (var pair in parameters)
        {
            RejectCredential(pair.Key);
            query[pair.Key] = pair.Value;
        }

        if (apiKey != null)
            query["key"] = apiKey;
        return uri.GetLeftPart(UriPartial.Path) + "?" + string.Join("&", query.Select(p => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value)));
    }

    private static void RejectCredential(string name)
    {
        if (new[]
        {
            "key",
            "access_token",
            "oauth_token",
            "authorization"
        }.Contains(name, StringComparer.OrdinalIgnoreCase))
            throw new PluginMisconfigurationException("Authentication parameters are supplied by the connection and cannot be overridden.");
    }
}
