using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.YouTube;
public sealed class LanguageDataHandler(InvocationContext context) : Invocable(context), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken) => YouTubeClient.RunAsync<IEnumerable<DataSourceItem>>(async () =>
    {
        var page = await Client.GetAsync<YouTubePage<PickerItem>>("i18nLanguages", new() { ["part"] = "snippet", ["hl"] = "en" });
        return page.Items.Select(x => new DataSourceItem(x.Id, x.Snippet.Name)).Where(x => string.IsNullOrWhiteSpace(context.SearchString) || x.DisplayName.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase) || x.Value.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase)).Take(50).ToList();
    });
}
