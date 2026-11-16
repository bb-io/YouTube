using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.YouTube;

public sealed class CaptionDataHandler(InvocationContext context, [ActionParameter] VideoInput input) : Invocable(context), IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken) =>
        YouTubeClient.RunAsync<IEnumerable<DataSourceItem>>(async () =>
        {
            if (string.IsNullOrWhiteSpace(input.VideoId))
                return [];

            Client.RequireOAuth();
            var page = await Client.GetAsync<YouTubePage<CaptionDto>>("captions", new()
            {
                ["part"] = "snippet",
                ["videoId"] = YouTubeUrls.ParseVideoId(input.VideoId)
            });
            var search = context.SearchString?.Trim();
            return page.Items.Select(item => new DataSourceItem(item.Id, Label(item)))
                .Where(item => string.IsNullOrWhiteSpace(search)
                    || item.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || item.Value.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Take(50)
                .ToList();
        });

    private static string Label(CaptionDto item)
    {
        var track = item.Snippet;
        var kind = track.TrackKind switch
        {
            "ASR" => "Automatic (ASR)",
            "standard" => "Standard",
            "forced" => "Forced",
            _ => "Unknown track type"
        };
        var status = track.Status switch
        {
            "serving" => "Ready",
            "syncing" => "Processing",
            "failed" => "Failed",
            _ => "Unknown status"
        };
        var name = string.IsNullOrWhiteSpace(track.Name) ? "Unnamed track" : track.Name;
        return $"{track.Language} — {name} — {(track.IsDraft ? "Draft" : "Not draft")} — {kind} — {status} — {item.Id}";
    }
}
