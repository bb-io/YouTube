using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;
using System.Globalization;
using System.Text.Json;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.YouTube;
[PollingEventList("Discovery")]
public sealed class AdditionalPollingEvents : Invocable
{
    public AdditionalPollingEvents(InvocationContext context) : base(context)
    {
    }

    internal AdditionalPollingEvents(YouTubeClient client) : base(client)
    {
    }

    [PollingEvent("On new comment on video", "Start a workflow when a new comment is added to the selected video, optionally including replies."), MultipleEvents]
    public Task<PollingEventResponse<DiscoveryMemory, List<VideoComment>>> OnNewComment(PollingEventRequest<DiscoveryMemory> request, [PollingEventParameter] CommentPollingInput input) => YouTubeClient.RunAsync<PollingEventResponse<DiscoveryMemory, List<VideoComment>>>(() => CommentsAsync(request.Memory, input));
    [PollingEvent("On new video matching search", "Start a workflow when a newly published public video matches a search. Search indexing can be delayed."), MultipleEvents]
    public Task<PollingEventResponse<DiscoveryMemory, List<Video>>> OnNewVideoBySearch(PollingEventRequest<DiscoveryMemory> request, [PollingEventParameter] SearchVideosInput input) => YouTubeClient.RunAsync<PollingEventResponse<DiscoveryMemory, List<Video>>>(() => SearchAsync(request.Memory, input));
    [PollingEvent("On new video in playlist", "Start a workflow when a new playlist entry matches the selected visibility."), MultipleEvents]
    public Task<PollingEventResponse<DiscoveryMemory, List<PlaylistVideoResponse>>> OnNewPlaylistVideo(PollingEventRequest<DiscoveryMemory> request, [PollingEventParameter] PlaylistPollingInput input) => YouTubeClient.RunAsync<PollingEventResponse<DiscoveryMemory, List<PlaylistVideoResponse>>>(() => PlaylistAsync(request.Memory, input));
    [PollingEvent("On new live stream", "Start a workflow when a broadcast is discovered, starts streaming, or both. Owner OAuth supports non-public broadcasts."), MultipleEvents]
    public Task<PollingEventResponse<DiscoveryMemory, List<LiveVideo>>> OnNewLiveStream(PollingEventRequest<DiscoveryMemory> request, [PollingEventParameter] LivePollingInput input) => YouTubeClient.RunAsync<PollingEventResponse<DiscoveryMemory, List<LiveVideo>>>(() => LiveAsync(request.Memory, input));
    public Task<List<JsonElement>> PagesAsync(string resource, Dictionary<string, string> parameters) => Client.Paginate<JsonElement>(resource, parameters);
    public async Task<PollingEventResponse<DiscoveryMemory, List<VideoComment>>> CommentsAsync(DiscoveryMemory? previous, CommentPollingInput input)
    {
        var id = YouTubeUrls.ParseVideoId(input.VideoId);
        var include = input.IncludeReplies ?? true;
        var key = id + ":" + include;
        var threads = await PagesAsync("commentThreads", new() { ["part"] = "snippet", ["fields"] = "nextPageToken,items(id,snippet(totalReplyCount,topLevelComment(id,snippet)))", ["videoId"] = id, ["maxResults"] = "100", ["order"] = "time", ["textFormat"] = "plainText" });
        var comments = new List<VideoComment>();
        foreach (var thread in threads)
        {
            var s = thread.GetProperty("snippet");
            var top = s.GetProperty("topLevelComment");
            comments.Add(MapComment(top, id));
            if (include && s.TryGetProperty("totalReplyCount", out var total) && total.GetInt32() > 0)
            {
                var replies = await PagesAsync("comments", new() { ["part"] = "snippet", ["fields"] = "nextPageToken,items(id,snippet)", ["parentId"] = YouTubeClient.Text(top, "id"), ["maxResults"] = "100", ["textFormat"] = "plainText" });
                comments.AddRange(replies.Select(r => MapComment(r, id)));
            }
        }

        return Batch(previous, key, comments.OrderBy(c => c.PublishedAt).Select(c => (c.CommentId, c)));
    }

    public async Task<PollingEventResponse<DiscoveryMemory, List<PlaylistVideoResponse>>> PlaylistAsync(DiscoveryMemory? previous, PlaylistPollingInput input)
    {
        var id = PublishingActions.PlaylistId(input.PlaylistId);
        var visibility = Visibility(input.VideoVisibility);
        if (visibility != "public")
        {
            api.RequireOAuth();
            var playlist = await api.GetAsync("playlists", new() { ["part"] = "snippet", ["id"] = id });
            var item = YouTubeClient.Items(playlist).SingleOrDefault();
            if (item.ValueKind != JsonValueKind.Object)
                throw new PluginMisconfigurationException("Playlist not found or not accessible.");
            await new VideoActions(api).VerifyOwnedChannelAsync(YouTubeClient.Text(item.GetProperty("snippet"), "channelId"));
        }

        var entries = await PagesAsync("playlistItems", new() { ["part"] = "snippet,contentDetails,status", ["playlistId"] = id, ["maxResults"] = "50" });
        var key = id + ":" + visibility;
        var seen = new HashSet<string>(previous?.Key == key ? previous.SeenIds : [], StringComparer.Ordinal);
        entries = entries.Where(e => !seen.Contains(YouTubeClient.Text(e, "id"))).ToList();
        var ids = entries.Where(e => e.TryGetProperty("contentDetails", out _)).Select(e => YouTubeClient.Text(e.GetProperty("contentDetails"), "videoId")).Where(v => v != "");
        var videos = (await new VideoActions(api).GetVideosAsync(ids)).ToDictionary(v => v.VideoId);
        var matches = new List<(string, PlaylistVideoResponse)>();
        foreach (var entry in entries)
        {
            if (!entry.TryGetProperty("contentDetails", out var details))
                continue;
            var vid = YouTubeClient.Text(details, "videoId");
            if (!videos.TryGetValue(vid, out var video) || !Matches(video.PrivacyStatus, visibility))
                continue;
            var s = entry.GetProperty("snippet");
            matches.Add((YouTubeClient.Text(entry, "id"), new() { PlaylistItemId = YouTubeClient.Text(entry, "id"), PlaylistId = id, VideoId = vid, Video = video, Position = s.TryGetProperty("position", out var pos) ? pos.GetInt32() : null }));
        }

        return Batch(previous, id + ":" + visibility, matches);
    }

    public async Task<PollingEventResponse<DiscoveryMemory, List<Video>>> SearchAsync(DiscoveryMemory? previous, SearchVideosInput input)
    {
        if (string.IsNullOrWhiteSpace(input.SearchTerm))
            throw new PluginMisconfigurationException("Search term is required for this event.");
        var copy = JsonSerializer.Deserialize<SearchVideosInput>(JsonSerializer.Serialize(input))!;
        copy.SortBy ??= "date";
        var key = JsonSerializer.Serialize(copy);
        var initial = previous == null || previous.Key != key;
        var result = await new VideoActions(api).SearchAsync(copy);
        var baseline = initial ? DateTime.UtcNow : previous!.StartedAt ?? DateTime.UtcNow;
        var candidates = initial ? result.Videos : result.Videos.Where(v => v.PublishedAt >= baseline).ToList();
        var response = Batch(previous, key, candidates.OrderBy(v => v.PublishedAt).Select(v => (v.VideoId, v)));
        response.Memory!.StartedAt = baseline;
        return response;
    }

    public async Task<PollingEventResponse<DiscoveryMemory, List<LiveVideo>>> LiveAsync(DiscoveryMemory? previous, LivePollingInput input)
    {
        var reference = input.Channel.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? YouTubeUrls.ParseChannel(input.Channel) : YouTubeUrls.ParseChannel("https://youtube.com/channel/" + input.Channel);
        var channel = await new VideoActions(api).ResolveChannelAsync(reference);
        var events = input.Events ?? "both";
        if (events is not ("both" or "created" or "started"))
            throw new PluginMisconfigurationException("Choose Created/discovered, Started or both.");
        var found = new List<LiveVideo>();
        if (input.OwnChannel == true)
        {
            api.RequireOAuth();
            await new VideoActions(api).VerifyOwnedChannelAsync(channel.Id);
            var broadcasts = await PagesAsync("liveBroadcasts", new() { ["part"] = "snippet,status", ["mine"] = "true", ["broadcastType"] = "all", ["maxResults"] = "50" });
            foreach (var b in broadcasts)
            {
                var snippet = b.GetProperty("snippet");
                if (YouTubeClient.Text(snippet, "channelId") != channel.Id)
                    continue;
                var state = YouTubeClient.Text(b.GetProperty("status"), "lifeCycleStatus");
                if (state is "complete" or "revoked")
                    continue;
                var live = new LiveVideo
                {
                    VideoId = YouTubeClient.Text(b, "id"),
                    Title = YouTubeClient.Text(snippet, "title"),
                    Status = state,
                    ScheduledStart = Date(snippet, "scheduledStartTime"),
                    ActualStart = Date(snippet, "actualStartTime")
                };
                AddLiveEvents(found, live, state == "live", events);
            }
        }
        else
        {
            var ids = new HashSet<string>();
            foreach (var state in new[]
            {
                "upcoming",
                "live"
            }

            )
            {
                var hits = await PagesAsync("search", new() { ["part"] = "snippet", ["type"] = "video", ["channelId"] = channel.Id, ["eventType"] = state, ["maxResults"] = "50" });
                foreach (var h in hits)
                    if (h.TryGetProperty("id", out var ident) && YouTubeClient.Text(ident, "kind") == "youtube#video")
                        ids.Add(YouTubeClient.Text(ident, "videoId"));
            }

            foreach (var chunk in ids.Where(x => x != "").Chunk(50))
            {
                var videos = await api.GetAsync("videos", new() { ["part"] = "snippet,status,liveStreamingDetails", ["id"] = string.Join(",", chunk) });
                foreach (var v in YouTubeClient.Items(videos))
                {
                    if (!v.TryGetProperty("status", out var st) || YouTubeClient.Text(st, "privacyStatus") != "public")
                        continue;
                    var s = v.GetProperty("snippet");
                    var state = YouTubeClient.Text(s, "liveBroadcastContent");
                    if (YouTubeClient.Text(s, "channelId") != channel.Id || state is not ("upcoming" or "live"))
                        continue;
                    var details = v.TryGetProperty("liveStreamingDetails", out var d) ? d : default;
                    AddLiveEvents(found, new() { VideoId = YouTubeClient.Text(v, "id"), Title = YouTubeClient.Text(s, "title"), Status = state, ScheduledStart = Date(details, "scheduledStartTime"), ActualStart = Date(details, "actualStartTime") }, state == "live", events);
                }
            }
        }

        return Batch(previous, channel.Id + ":" + events + ":" + (input.OwnChannel ?? false), found.Select(v => (v.VideoId + ":" + v.EventType, v)));
    }

    private YouTubeClient api => Client;

    private static void AddLiveEvents(List<LiveVideo> all, LiveVideo video, bool started, string filter)
    {
        if (filter is "both" or "created")
            all.Add(new() { VideoId = video.VideoId, Title = video.Title, Status = video.Status, ScheduledStart = video.ScheduledStart, ActualStart = video.ActualStart, EventType = "created" });
        if (started && filter is "both" or "started")
            all.Add(new() { VideoId = video.VideoId, Title = video.Title, Status = video.Status, ScheduledStart = video.ScheduledStart, ActualStart = video.ActualStart, EventType = "started" });
    }

    private static VideoComment MapComment(JsonElement comment, string videoId)
    {
        var s = comment.GetProperty("snippet");
        return new()
        {
            CommentId = YouTubeClient.Text(comment, "id"),
            ParentCommentId = YouTubeClient.Text(s, "parentId"),
            VideoId = videoId,
            Author = YouTubeClient.Text(s, "authorDisplayName"),
            Text = YouTubeClient.Text(s, "textOriginal")is var text && text != "" ? text : YouTubeClient.Text(s, "textDisplay"),
            PublishedAt = Date(s, "publishedAt") ?? default,
            UpdatedAt = Date(s, "updatedAt"),
            LikeCount = s.TryGetProperty("likeCount", out var likes) ? likes.GetInt64() : 0
        };
    }

    private static DateTime? Date(JsonElement s, string name) => s.ValueKind == JsonValueKind.Object && DateTime.TryParse(YouTubeClient.Text(s, name), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d) ? d : null;
    private static string Visibility(string? value)
    {
        value ??= "public";
        if (value is not ("public" or "private" or "unlisted" or "all"))
            throw new PluginMisconfigurationException("Choose a supported Video visibility.");
        return value;
    }

    private static bool Matches(string status, string filter) => status is "public" or "private" or "unlisted" && (filter == "all" || status == filter);
    private static PollingEventResponse<DiscoveryMemory, List<T>> Batch<T>(DiscoveryMemory? previous, string key, IEnumerable<(string Id, T Item)> source)
    {
        var items = source.DistinctBy(x => x.Id).ToList();
        var initial = previous == null || previous.Key != key;
        var seen = new HashSet<string>(initial ? [] : previous!.SeenIds, StringComparer.Ordinal);
        var result = initial ? [] : items.Where(x => !seen.Contains(x.Id)).Take(100).Select(x => x.Item).ToList();
        seen.UnionWith(initial ? items.Select(x => x.Id) : items.Where(x => !seen.Contains(x.Id)).Take(100).Select(x => x.Id));
        return new()
        {
            FlyBird = result.Count > 0,
            Result = result,
            Memory = new()
            {
                Key = key,
                SeenIds = seen.ToList(),
                StartedAt = previous?.StartedAt
            }
        };
    }
}
