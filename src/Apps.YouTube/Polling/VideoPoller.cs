using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Polling;

namespace Apps.YouTube;

public sealed class VideoPoller(IYouTubeApi api)
{
    private const int MaximumChannels = 20;
    private const int MaximumVideos = 100;

    public async Task<PollingEventResponse<PollingMemory, List<Video>>> PollAsync(
        PollingMemory? previous,
        string channelUrl,
        string? videoVisibility = null,
        IEnumerable<string>? additionalChannelUrls = null)
    {
        var visibility = string.IsNullOrWhiteSpace(videoVisibility) ? "public" : videoVisibility.Trim();
        if (visibility is not ("public" or "unlisted" or "private" or "all"))
            throw new PluginMisconfigurationException("Choose Public only, Unlisted only, Private only, or All accessible videos from Video visibility.");
        if (visibility != "public" && !api.HasOAuth)
            throw new PluginMisconfigurationException("Unlisted/private polling requires the YouTube channel management OAuth connection for every watched channel. Select OAuth or choose Public only.");

        var references = new[] { channelUrl }
            .Concat(additionalChannelUrls ?? [])
            .Select(YouTubeUrls.ParseChannel)
            .DistinctBy(channel => channel.Key, StringComparer.Ordinal)
            .ToList();
        if (references.Count > MaximumChannels)
            throw new PluginMisconfigurationException("Watch up to 20 channels per Bird. Remove extra channel URLs or use another Bird.");

        var stored = GetStoredChannels(previous);
        var channels = new List<VideoChannelMemory>();
        var pending = new List<List<string>>();
        var resolvedIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var reference in references)
        {
            var old = stored.FirstOrDefault(channel => channel.ChannelKey == reference.Key);
            var resolved = old == null
                ? await api.ResolveChannelAsync(reference)
                : new ResolvedChannel(old.ChannelId, old.UploadsPlaylistId);
            if (!resolvedIds.Add(resolved.Id))
                continue;

            old ??= stored.FirstOrDefault(channel => channel.ChannelId == resolved.Id);
            if (visibility != "public")
                await api.VerifyOwnedChannelAsync(resolved.Id);

            var uploads = await api.ListUploadsAsync(resolved.UploadsPlaylistId);
            var currentIds = uploads.Where(video => Matches(video.PrivacyStatus, visibility))
                .Select(video => video.VideoId)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var seen = new HashSet<string>(old?.SeenVideoIds ?? [], StringComparer.Ordinal);
            var previousVisibility = string.IsNullOrWhiteSpace(old?.VideoVisibility) ? "public" : old.VideoVisibility;
            var baseline = old == null || previousVisibility != visibility;
            if (baseline)
                seen.UnionWith(currentIds);

            channels.Add(new VideoChannelMemory
            {
                ChannelKey = reference.Key,
                ChannelId = resolved.Id,
                UploadsPlaylistId = resolved.UploadsPlaylistId,
                VideoVisibility = visibility,
                SeenVideoIds = seen.ToList()
            });
            pending.Add(baseline ? [] : currentIds.Where(id => !seen.Contains(id)).Reverse().Take(MaximumVideos).ToList());
        }

        var candidates = new List<string>();
        for (var index = 0; candidates.Count < MaximumVideos; index++)
        {
            var found = false;
            foreach (var queue in pending)
            {
                if (index >= queue.Count)
                    continue;
                candidates.Add(queue[index]);
                found = true;
                if (candidates.Count == MaximumVideos)
                    break;
            }
            if (!found)
                break;
        }

        var videos = candidates.Count == 0 ? [] : await api.GetVideosAsync(candidates);
        var byId = videos.Where(video => Matches(video.PrivacyStatus, visibility))
            .ToDictionary(video => video.VideoId, StringComparer.Ordinal);
        var result = candidates.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        var delivered = result.Select(video => video.VideoId).ToHashSet(StringComparer.Ordinal);
        for (var index = 0; index < channels.Count; index++)
            channels[index].SeenVideoIds.AddRange(pending[index].Where(delivered.Contains));

        var primary = channels[0];
        return new PollingEventResponse<PollingMemory, List<Video>>
        {
            FlyBird = result.Count > 0,
            Memory = new PollingMemory
            {
                ChannelKey = primary.ChannelKey,
                ChannelId = primary.ChannelId,
                UploadsPlaylistId = primary.UploadsPlaylistId,
                SeenVideoIds = primary.SeenVideoIds.ToList(),
                VideoVisibility = visibility,
                Channels = channels
            },
            Result = result
        };
    }

    private static List<VideoChannelMemory> GetStoredChannels(PollingMemory? previous)
    {
        if (previous == null)
            return [];
        if (previous.Channels is { Count: > 0 })
            return previous.Channels;
        return [new VideoChannelMemory
        {
            ChannelKey = previous.ChannelKey,
            ChannelId = previous.ChannelId,
            UploadsPlaylistId = previous.UploadsPlaylistId,
            SeenVideoIds = previous.SeenVideoIds,
            VideoVisibility = previous.VideoVisibility
        }];
    }

    private static bool Matches(string status, string filter) => status is "public" or "unlisted" or "private" && (filter == "all" || status == filter);
}
