using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.YouTube;

public sealed class ChannelIdDataHandler(InvocationContext context, [ActionParameter] ReportContext input) : Invocable(context), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken) =>
        YouTubeClient.RunAsync<IEnumerable<DataSourceItem>>(async () =>
        {
            if (!Client.HasOAuth || input.ReportType == "contentOwner")
                return [];
            var channels = await Client.Paginate<PickerItem>("channels", new()
            {
                ["part"] = "snippet", ["mine"] = "true", ["maxResults"] = "50"
            }, 50);
            var search = context.SearchString?.Trim();
            return channels.Select(channel => new DataSourceItem(channel.Id, $"{channel.Snippet.Title} — {channel.Id}"))
                .Where(item => string.IsNullOrWhiteSpace(search) || item.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) || item.Value.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Take(50).ToList();
        });
}
