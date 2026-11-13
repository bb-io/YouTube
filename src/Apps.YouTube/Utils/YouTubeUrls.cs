using System.Text.RegularExpressions;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.YouTube;
public static class YouTubeUrls
{
    public static ChannelReference ParseChannel(string value)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != "https" || !IsYouTubeHost(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo) || !uri.IsDefaultPort)
            throw new PluginMisconfigurationException("Enter an HTTPS YouTube channel URL, such as https://www.youtube.com/@Blackbirdio.");
        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.UnescapeDataString).ToArray();
        var tabs = new[]
        {
            "videos",
            "shorts",
            "streams",
            "featured",
            "playlists",
            "community",
            "about",
            "podcasts"
        };
        if (parts.Length is 1 or 2 && parts[0].StartsWith('@') && parts[0].Length > 1 && (parts.Length == 1 || tabs.Contains(parts[1])))
            return new("forHandle", parts[0]);
        if (parts.Length is 2 or 3 && (parts.Length == 2 || tabs.Contains(parts[2])))
        {
            if (parts[0] == "channel" && Regex.IsMatch(parts[1], "^UC[A-Za-z0-9_-]{22}$"))
                return new("id", parts[1]);
            if (parts[0] == "user" && !string.IsNullOrWhiteSpace(parts[1]))
                return new("forUsername", parts[1]);
        }

        throw new PluginMisconfigurationException("Use the channel's /@handle, /channel/UC..., or /user/username URL. Legacy /c/ custom URLs cannot be resolved reliably through the Data API; copy the current @handle URL from YouTube.");
    }

    public static string ParseVideoId(string value)
    {
        value = value?.Trim() ?? string.Empty;
        if (Regex.IsMatch(value, "^[A-Za-z0-9_-]{11}$"))
            return value;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo) && uri.IsDefaultPort)
        {
            var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string? id = null;
            if ((uri.Host is "youtu.be" or "www.youtu.be") && parts.Length == 1)
                id = parts[0];
            if (IsYouTubeHost(uri.Host))
            {
                if (parts.Length == 2 && parts[0] is "shorts" or "embed" or "live")
                    id = parts[1];
                if (parts.Length == 1 && parts[0] == "watch")
                    id = uri.Query.TrimStart('?').Split('&').Select(x => x.Split('=', 2)).Where(x => x.Length == 2 && x[0] == "v").Select(x => Uri.UnescapeDataString(x[1])).FirstOrDefault();
            }

            if (id != null && Regex.IsMatch(id, "^[A-Za-z0-9_-]{11}$"))
                return id;
        }

        throw new PluginMisconfigurationException("Enter an 11-character YouTube video ID or a valid HTTPS YouTube video URL.");
    }

    private static bool IsYouTubeHost(string host) => host is "youtube.com" or "www.youtube.com" or "m.youtube.com";
}
