using System.Text;
using System.Text.RegularExpressions;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.YouTube;
public static class InputValidator
{
    public static int MaximumResults(int? value)
    {
        var maximum = value ?? 100;
        if (maximum is < 1 or > 10000)
            throw new PluginMisconfigurationException("Maximum results must be between 1 and 10,000.");
        return maximum;
    }

    public static string Visibility(string? value, string fallback = "public")
    {
        value = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        if (value is not ("public" or "unlisted" or "private" or "all"))
            throw new PluginMisconfigurationException("Choose a valid Video visibility option.");
        return value;
    }

    public static string Privacy(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new PluginMisconfigurationException("Choose a valid Privacy status.");
        var privacy = Visibility(value);
        if (privacy == "all")
            throw new PluginMisconfigurationException("Privacy status must be private, unlisted or public.");
        return privacy;
    }

    public static DateTime Utc(DateTime value) => value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
    public static bool Matches(string title, string description, string? search) => string.IsNullOrWhiteSpace(search) || title.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) || description.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase);
    public static void PublicationRange(DateTime? after, DateTime? before)
    {
        if (after.HasValue && before.HasValue && Utc(after.Value) > Utc(before.Value))
            throw new PluginMisconfigurationException("Published after must be earlier than or equal to Published before.");
    }

    public static void VideoText(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length > 100 || title.IndexOfAny(['<', '>']) >= 0)
            throw new PluginMisconfigurationException("Supply a Title of 1-100 characters without angle brackets.");
        if (Encoding.UTF8.GetByteCount(description) > 5000 || description.IndexOfAny(['<', '>']) >= 0)
            throw new PluginMisconfigurationException("Description must fit 5,000 UTF-8 bytes without angle brackets.");
    }

    public static void Category(string value)
    {
        if (!Regex.IsMatch(value, "^[0-9]+$"))
            throw new PluginMisconfigurationException("Choose a valid Category ID from the dropdown or enter its numeric ID.");
    }

    public static void PlaylistTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new PluginMisconfigurationException("Supply a non-empty playlist Title.");
    }

    public static string PlaylistItemId(string value)
    {
        value = value?.Trim() ?? "";
        if (!Regex.IsMatch(value, "^[A-Za-z0-9_-]{2,500}$"))
            throw new PluginMisconfigurationException("Supply Playlist item ID from Search playlist videos or Add video to playlist. A Video ID is not a playlist entry ID.");
        return value;
    }
}
