using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.YouTube;

public sealed class PlaylistDataHandler(InvocationContext context) : Invocable(context), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken) =>
        YouTubeClient.RunAsync<IEnumerable<DataSourceItem>>(async () =>
        {
            if (!Client.HasOAuth)
                return [];

            var search = context.SearchString?.Trim();
            var matches = new List<DataSourceItem>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            await Client.Paginate<PickerItem>("playlists", new()
            {
                ["part"] = "snippet",
                ["mine"] = "true",
                ["maxResults"] = "50"
            }, continueAfterPage: page =>
            {
                foreach (var item in page)
                {
                    if (!seen.Add(item.Id))
                        continue;
                    if (!string.IsNullOrWhiteSpace(search)
                        && !item.Snippet.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                        && !item.Id.Contains(search, StringComparison.OrdinalIgnoreCase))
                        continue;
                    matches.Add(new DataSourceItem(item.Id, $"{item.Snippet.Title} — {item.Id}"));
                    if (matches.Count == 50)
                        return Task.FromResult(false);
                }
                return Task.FromResult(true);
            }, collectItems: false);
            return matches;
        });
}
