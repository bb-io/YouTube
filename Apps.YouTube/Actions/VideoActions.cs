using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Blackbird.Applications.Sdk.Common.Authentication;

namespace Apps.YouTube;
[ActionList("Videos")]
public sealed class VideoActions : Invocable, IYouTubeApi
{
    public VideoActions(InvocationContext context) : base(context)
    {
    }

    internal VideoActions(YouTubeClient client) : base(client)
    {
    }

    public bool HasOAuth => Client.HasOAuth;

    [Action("Search videos", Description = "Search public videos by keyword, channel and filters. Without a keyword, retrieve public channel uploads.")]
    public Task<SearchVideosResponse> SearchVideos([ActionParameter] SearchVideosInput input) => YouTubeClient.RunAsync<SearchVideosResponse>(() => SearchAsync(input));
    [Action("Get video", Description = "Get a video’s metadata and watch URL using its ID or URL.")]
    public Task<Video> GetVideoUrl([ActionParameter] VideoInput input) => YouTubeClient.RunAsync<Video>(async () =>
    {
        var id = YouTubeUrls.ParseVideoId(input.VideoId);
        var videos = await GetVideosAsync([id]);
        return videos.SingleOrDefault() ?? throw new PluginMisconfigurationException("The video was not found or is not accessible with this connection. Check the video ID and use an authorized channel management OAuth connection for private videos.");
    });
    public async Task<SearchVideosResponse> SearchAsync(SearchVideosInput input)
    {
        var term = input.SearchTerm?.Trim();
        var channelUrl = input.ChannelUrl?.Trim();
        if (string.IsNullOrEmpty(term) && string.IsNullOrEmpty(channelUrl) && string.IsNullOrWhiteSpace(input.ChannelId))
            throw new PluginMisconfigurationException("Enter a Channel URL, a Search term, or both.");
        var limit = input.MaximumResults ?? 100;
        if (limit is < 1 or > 10000)
            throw new PluginMisconfigurationException("Maximum results must be between 1 and 10,000.");
        var order = Option(input.SortBy, string.IsNullOrEmpty(term) ? "date" : "relevance", ["relevance", "date", "title", "viewCount", "rating"], "Sort by");
        var caption = Option(input.CaptionAvailability, "any", ["any", "closedCaption", "none"], "Caption availability");
        var duration = Option(input.Duration, "any", ["any", "short", "medium", "long"], "Duration");
        var after = Utc(input.PublishedAfter);
        var before = Utc(input.PublishedBefore);
        if (after.HasValue && before.HasValue && after > before)
            throw new PluginMisconfigurationException("Published after must be earlier than or equal to Published before.");
        if (!string.IsNullOrWhiteSpace(channelUrl) && !string.IsNullOrWhiteSpace(input.ChannelId))
            throw new PluginMisconfigurationException("Supply Channel URL or Channel ID, not both.");
        var definition = Option(input.Definition, "any", ["any", "high", "standard"], "Video definition");
        var dimension = Option(input.Dimension, "any", ["any", "2d", "3d"], "Video dimension");
        var license = Option(input.License, "any", ["any", "creativeCommon", "youtube"], "Video license");
        var safe = Option(input.SafeSearch, "moderate", ["moderate", "none", "strict"], "Safe search");
        var region = input.RegionCode?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(region) && !System.Text.RegularExpressions.Regex.IsMatch(region, "^[A-Z]{2}$"))
            throw new PluginMisconfigurationException("Region code must be a two-letter country code.");
        var language = string.IsNullOrWhiteSpace(input.RelevanceLanguage) ? null : LocalizationActions.LanguageTag(input.RelevanceLanguage);
        if (!string.IsNullOrWhiteSpace(input.CategoryId) && !System.Text.RegularExpressions.Regex.IsMatch(input.CategoryId, "^[0-9]+$"))
            throw new PluginMisconfigurationException("Video category ID must be numeric.");
        ResolvedChannel? channel = null;
        if (!string.IsNullOrWhiteSpace(input.ChannelId))
            channel = await ResolveChannelAsync(YouTubeUrls.ParseChannel("https://youtube.com/channel/" + input.ChannelId.Trim()));
        else if (!string.IsNullOrEmpty(channelUrl))
            channel = await ResolveChannelAsync(YouTubeUrls.ParseChannel(channelUrl));
        var indexed = order == "rating" || !string.IsNullOrEmpty(term) || definition != "any" || dimension != "any" || license != "any" || input.SafeSearch != null || !string.IsNullOrEmpty(region) || language != null || !string.IsNullOrWhiteSpace(input.CategoryId);
        bool Matches(Video v) => v.PrivacyStatus == "public" && (channel == null || v.ChannelId == channel.Id) && (!after.HasValue || v.PublishedAt >= after.Value) && (!before.HasValue || v.PublishedAt <= before.Value) && (caption == "any" || v.HasCaptions == (caption == "closedCaption")) && (duration == "any" || v.DurationSeconds.HasValue && duration switch
        {
            "short" => v.DurationSeconds < 240,
            "medium" => v.DurationSeconds >= 240 && v.DurationSeconds <= 1200,
            "long" => v.DurationSeconds > 1200,
            _ => false
        });
        if (!indexed)
        {
            var ids = await ListPublicUploadIdsAsync(channel!.UploadsPlaylistId);
            var videos = (await GetVideosAsync(ids)).Where(Matches);
            var sorted = order switch
            {
                "title" => videos.OrderBy(v => v.Title, StringComparer.OrdinalIgnoreCase).ThenBy(v => v.VideoId, StringComparer.Ordinal),
                "viewCount" => videos.OrderByDescending(v => v.ViewCount ?? -1).ThenBy(v => v.VideoId, StringComparer.Ordinal),
                _ => videos.OrderByDescending(v => v.PublishedAt).ThenBy(v => v.VideoId, StringComparer.Ordinal)};
            var matches = sorted.Take(limit + 1).ToList();
            return new()
            {
                Videos = matches.Take(limit).ToList(),
                MoreResultsAvailable = matches.Count > limit
            };
        }

        var parameters = new Dictionary<string, string>
        {
            ["part"] = "snippet",
            ["type"] = "video",
            ["order"] = order,
            ["maxResults"] = "50",
            ["videoCaption"] = caption,
            ["videoDuration"] = duration
        };
        if (!string.IsNullOrEmpty(term))
            parameters["q"] = term;
        parameters["videoDefinition"] = definition;
        parameters["videoDimension"] = dimension;
        parameters["videoLicense"] = license;
        parameters["safeSearch"] = safe;
        if (!string.IsNullOrEmpty(region))
            parameters["regionCode"] = region;
        if (language != null)
            parameters["relevanceLanguage"] = language;
        if (!string.IsNullOrWhiteSpace(input.CategoryId))
            parameters["videoCategoryId"] = input.CategoryId;
        if (channel != null)
            parameters["channelId"] = channel.Id;
        if (after.HasValue)
            parameters["publishedAfter"] = after.Value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
        if (before.HasValue)
            parameters["publishedBefore"] = before.Value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
        var results = new List<Video>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        var sourceCount = 0;
        var ceiling = false;
        for (var page = 0;; page++)
        {
            if (page >= 1000)
                throw new PluginApplicationException("YouTube search exceeded the defensive pagination limit. Narrow the search term or publication dates.");
            var data = await Client.GetAsync("search", parameters);
            var items = YouTubeClient.Items(data);
            var ids = new List<string>();
            foreach (var item in items)
            {
                if (!item.TryGetProperty("id", out var id) || YouTubeClient.Text(id, "kind") != "youtube#video")
                    continue;
                var videoId = YouTubeClient.Text(id, "videoId");
                if (!string.IsNullOrEmpty(videoId) && seen.Add(videoId))
                    ids.Add(videoId);
            }

            sourceCount += ids.Count;
            var hydrated = (await GetVideosAsync(ids)).ToDictionary(v => v.VideoId, StringComparer.Ordinal);
            results.AddRange(ids.Where(hydrated.ContainsKey).Select(id => hydrated[id]).Where(Matches));
            ceiling = channel != null && sourceCount >= 500;
            if (results.Count > limit || ceiling)
                break;
            var next = YouTubeClient.Text(data, "nextPageToken");
            if (string.IsNullOrEmpty(next))
                break;
            if (!tokens.Add(next))
                throw new PluginApplicationException("YouTube repeated a search pagination token. Retry the search or narrow the filters.");
            parameters["pageToken"] = next;
        }

        return new()
        {
            Videos = results.Take(limit).ToList(),
            MoreResultsAvailable = results.Count > limit,
            SearchLimitReached = ceiling
        };
    }

    public async Task<ResolvedChannel> ResolveChannelAsync(ChannelReference channel)
    {
        var data = await Client.GetAsync("channels", new() { ["part"] = "contentDetails", [channel.Filter] = channel.Value });
        var items = YouTubeClient.Items(data);
        if (items.Count == 0)
            throw new PluginMisconfigurationException("No public channel was found. Check the channel URL and use its current @handle or /channel/UC... URL.");
        var item = items[0];
        var uploads = item.GetProperty("contentDetails").GetProperty("relatedPlaylists");
        var playlistId = YouTubeClient.Text(uploads, "uploads");
        if (string.IsNullOrEmpty(playlistId))
            throw new PluginMisconfigurationException("The channel does not expose an uploads playlist. Choose another public channel.");
        return new(YouTubeClient.Text(item, "id"), playlistId);
    }

    public async Task VerifyOwnedChannelAsync(string channelId)
    {
        Client.RequireOAuth();
        var parameters = new Dictionary<string, string>
        {
            ["part"] = "id",
            ["mine"] = "true",
            ["maxResults"] = "50"
        };
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        for (var page = 0; page < 1000; page++)
        {
            var data = await Client.GetAsync("channels", parameters);
            if (YouTubeClient.Items(data).Any(c => YouTubeClient.Text(c, "id") == channelId))
                return;
            var token = YouTubeClient.Text(data, "nextPageToken");
            if (string.IsNullOrEmpty(token))
                throw new PluginMisconfigurationException("The selected OAuth connection does not own the specified channel. Reconnect as the owning YouTube/Brand Account, or choose Public only.");
            if (!tokens.Add(token))
                throw new PluginApplicationException("YouTube repeated an ownership pagination token. Retry the poll.");
            parameters["pageToken"] = token;
        }

        throw new PluginApplicationException("YouTube channel ownership lookup exceeded its pagination limit.");
    }

    public async Task<List<string>> ListPublicUploadIdsAsync(string playlistId) => (await ListUploadsAsync(playlistId)).Where(v => v.PrivacyStatus == "public").Select(v => v.VideoId).ToList();
    public async Task<List<UploadVideo>> ListUploadsAsync(string playlistId)
    {
        var ids = new List<UploadVideo>();
        string? token = null;
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        for (var page = 0;; page++)
        {
            if (page >= 1000)
                throw new PluginApplicationException("The channel exceeds this app's 50,000-item scan limit. Polling memory was not advanced. Use a smaller channel or extend the app's scan strategy.");
            var parameters = new Dictionary<string, string>
            {
                ["part"] = "contentDetails,status",
                ["playlistId"] = playlistId,
                ["maxResults"] = "50"
            };
            if (token != null)
                parameters["pageToken"] = token;
            var data = await Client.GetAsync("playlistItems", parameters);
            foreach (var item in YouTubeClient.Items(data))
            {
                if (!item.TryGetProperty("status", out var status))
                    continue;
                var privacy = YouTubeClient.Text(status, "privacyStatus");
                if (privacy is not ("public" or "unlisted" or "private"))
                    continue;
                if (item.TryGetProperty("contentDetails", out var details))
                {
                    var id = YouTubeClient.Text(details, "videoId");
                    if (!string.IsNullOrEmpty(id))
                        ids.Add(new(id, privacy));
                }
            }

            token = YouTubeClient.Text(data, "nextPageToken");
            if (string.IsNullOrEmpty(token))
                break;
            if (!tokens.Add(token))
                throw new PluginApplicationException("YouTube repeated a pagination token. Retry the poll; polling memory was not advanced.");
        }

        return ids.DistinctBy(v => v.VideoId, StringComparer.Ordinal).ToList();
    }

    public async Task<List<Video>> GetVideosAsync(IEnumerable<string> ids)
    {
        var videos = new List<Video>();
        foreach (var batch in ids.Distinct(StringComparer.Ordinal).Chunk(50))
        {
            var page = await Client.GetAsync<YouTubePage<VideoDto>>("videos", new() { ["part"] = "snippet,status,contentDetails,statistics", ["id"] = string.Join(",", batch) });
            videos.AddRange(page.Items.Select(item => new Video { VideoId = item.Id, VideoUrl = $"https://www.youtube.com/watch?v={item.Id}", Title = item.Snippet.Title, Description = item.Snippet.Description, ChannelId = item.Snippet.ChannelId, ChannelTitle = item.Snippet.ChannelTitle, PublishedAt = item.Snippet.PublishedAt.ToUniversalTime(), PrivacyStatus = item.Status.PrivacyStatus, DurationSeconds = string.IsNullOrEmpty(item.ContentDetails.Duration) ? null : ParseDuration(item.ContentDetails.Duration), HasCaptions = bool.TryParse(item.ContentDetails.Caption, out var captions) ? captions : null, ViewCount = item.Statistics.ViewCount }));
        }

        return videos;
    }

    private static string Option(string? value, string fallback, string[] allowed, string name)
    {
        value = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        if (!allowed.Contains(value, StringComparer.Ordinal))
            throw new PluginMisconfigurationException($"Choose a valid {name} value from the dropdown.");
        return value;
    }

    private static DateTime? Utc(DateTime? value) => value.HasValue ? value.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value.Value.ToUniversalTime() : null;
    private static double ParseDuration(string duration)
    {
        try
        {
            return System.Xml.XmlConvert.ToTimeSpan(duration).TotalSeconds;
        }
        catch (FormatException)
        {
            throw new PluginApplicationException("YouTube returned an invalid video duration. Retry the operation.");
        }
    }
}
