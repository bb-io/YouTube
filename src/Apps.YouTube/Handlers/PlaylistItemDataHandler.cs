using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.YouTube;

public sealed class PlaylistItemDataHandler(InvocationContext context, [ActionParameter] PlaylistIdentifier input) : Invocable(context), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken) =>
        YouTubeClient.RunAsync<IEnumerable<DataSourceItem>>(async () =>
        {
            if (string.IsNullOrWhiteSpace(input.PlaylistId))
                return [];

            var id = PublishingActions.PlaylistId(input.PlaylistId);
            var search = context.SearchString?.Trim();
            var matches = new List<DataSourceItem>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            await Client.Paginate<PlaylistEntryDto>("playlistItems", new()
            {
                ["part"] = "snippet,contentDetails",
                ["playlistId"] = id,
                ["maxResults"] = "50"
            }, continueAfterPage: page =>
            {
                foreach (var item in page)
                {
                    if (!seen.Add(item.Id))
                        continue;
                    var label = $"{item.Snippet.Title} — video {item.ContentDetails.VideoId} — entry {item.Id}";
                    if (!string.IsNullOrWhiteSpace(search)
                        && !item.Snippet.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                        && !item.Id.Contains(search, StringComparison.OrdinalIgnoreCase)
                        && !item.ContentDetails.VideoId.Contains(search, StringComparison.OrdinalIgnoreCase))
                        continue;
                    matches.Add(new DataSourceItem(item.Id, label));
                    if (matches.Count == 50)
                        return Task.FromResult(false);
                }
                return Task.FromResult(true);
            }, collectItems: false);
            return matches;
        });
}
