using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.YouTube;

public sealed class VideoDataHandler(InvocationContext context) : Invocable(context), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken) =>
        YouTubeClient.RunAsync<IEnumerable<DataSourceItem>>(async () =>
        {
            if (!Client.HasOAuth)
                return [];

            var channels = await Client.Paginate<ChannelInventoryDto>("channels", new()
            {
                ["part"] = "contentDetails",
                ["mine"] = "true",
                ["maxResults"] = "50"
            }, 2);
            if (channels.Count != 1)
                throw new PluginMisconfigurationException("Reconnect using the intended YouTube or Brand Account to choose videos from one channel. You can also enter or map Video ID directly.");

            var channel = channels[0];
            var search = context.SearchString?.Trim();
            var matches = new List<DataSourceItem>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            await Client.Paginate<PlaylistEntryDto>("playlistItems", new()
            {
                ["part"] = "contentDetails",
                ["playlistId"] = channel.ContentDetails.RelatedPlaylists.Uploads,
                ["maxResults"] = "50"
            }, continueAfterPage: async page =>
            {
                var ids = page.Select(entry => entry.ContentDetails.VideoId)
                    .Where(id => !string.IsNullOrWhiteSpace(id) && seen.Add(id))
                    .ToList();
                if (ids.Count == 0)
                    return true;

                var videos = await Client.GetAsync<YouTubePage<VideoDto>>("videos", new()
                {
                    ["part"] = "snippet,status",
                    ["id"] = string.Join(",", ids)
                });
                var byId = videos.Items.Where(video => video.Snippet.ChannelId == channel.Id)
                    .ToDictionary(video => video.Id, StringComparer.Ordinal);
                foreach (var id in ids)
                {
                    if (!byId.TryGetValue(id, out var video))
                        continue;
                    var visibility = string.IsNullOrWhiteSpace(video.Status.PrivacyStatus) ? "Unknown visibility" : video.Status.PrivacyStatus;
                    var label = $"{video.Snippet.Title} — {visibility} — {video.Id}";
                    if (!string.IsNullOrWhiteSpace(search)
                        && !video.Snippet.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                        && !video.Id.Contains(search, StringComparison.OrdinalIgnoreCase))
                        continue;
                    matches.Add(new DataSourceItem(video.Id, label));
                    if (matches.Count == 50)
                        return false;
                }
                return true;
            }, collectItems: false);
            return matches;
        });
}
