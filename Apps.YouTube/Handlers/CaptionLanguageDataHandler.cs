using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.YouTube;

public sealed class CaptionLanguageDataHandler(InvocationContext context, [ActionParameter] CaptionLanguageContext input) : Invocable(context), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken) =>
        YouTubeClient.RunAsync<IEnumerable<DataSourceItem>>(async () =>
        {
            if (string.IsNullOrWhiteSpace(input.VideoId))
                return [];
            Client.RequireOAuth();
            var captions = await Client.GetAsync<YouTubePage<CaptionDto>>("captions", new()
            {
                ["part"] = "snippet", ["videoId"] = YouTubeUrls.ParseVideoId(input.VideoId)
            });
            var groups = captions.Items.Where(item => !string.IsNullOrWhiteSpace(item.Snippet.Language) && (input.IncludeDrafts != false || !item.Snippet.IsDraft))
                .GroupBy(item => item.Snippet.Language, StringComparer.OrdinalIgnoreCase).ToList();
            if (groups.Count == 0)
                return [];
            var languages = await Client.GetAsync<YouTubePage<PickerItem>>("i18nLanguages", new() { ["part"] = "snippet", ["hl"] = "en" });
            var names = languages.Items.ToDictionary(item => item.Id, item => item.Snippet.Name, StringComparer.OrdinalIgnoreCase);
            var search = context.SearchString?.Trim();
            return groups.Select(group => new DataSourceItem(group.Key, $"{names.GetValueOrDefault(group.Key, group.Key)} — {group.Key} — {group.Count()} track(s)"))
                .Where(item => string.IsNullOrWhiteSpace(search) || item.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) || item.Value.Contains(search, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase).Take(50).ToList();
        });
}
