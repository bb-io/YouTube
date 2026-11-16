using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.YouTube;
public sealed class CategoryDataHandler(InvocationContext context, [ActionParameter] CategoryContext input) : Invocable(context), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken) => YouTubeClient.RunAsync<IEnumerable<DataSourceItem>>(async () =>
    {
        var page = await Client.GetAsync<YouTubePage<PickerItem>>("videoCategories", new() { ["part"] = "snippet", ["regionCode"] = Region(input.RegionCode), ["hl"] = "en" });
        return page.Items.Where(x => x.Snippet.Assignable).Select(x => new DataSourceItem(x.Id, x.Snippet.Title)).Where(x => string.IsNullOrWhiteSpace(context.SearchString) || x.DisplayName.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase) || x.Value.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase)).Take(50).ToList();
    });
    private static string Region(string? value)
    {
        value = string.IsNullOrWhiteSpace(value) ? "US" : value.Trim().ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Z]{2}$"))
            throw new Blackbird.Applications.Sdk.Common.Exceptions.PluginMisconfigurationException("Select a two-letter Region code.");
        return value;
    }
}
