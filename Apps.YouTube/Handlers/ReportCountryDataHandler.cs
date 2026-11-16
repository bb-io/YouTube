using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.YouTube;

public sealed class ReportCountryDataHandler(InvocationContext context, [ActionParameter] ReportContext input) : Invocable(context), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken) =>
        YouTubeClient.RunAsync<IEnumerable<DataSourceItem>>(async () =>
        {
            if (!Client.HasOAuth || !ReportRules.CanFilterCountry(input))
                return [];
            var page = await Client.GetAsync<YouTubePage<PickerItem>>("i18nRegions", new() { ["part"] = "snippet", ["hl"] = "en" });
            var search = context.SearchString?.Trim();
            return page.Items.Select(item => new DataSourceItem(item.Id, item.Snippet.Name + " — " + item.Id))
                .Where(item => string.IsNullOrWhiteSpace(search) || item.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) || item.Value.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Take(50).ToList();
        });
}
