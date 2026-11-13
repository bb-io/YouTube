using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class LivePollingInput
{
    [Display("Channel URL or ID")]
    public string Channel { get; set; } = "";

    [Display("Events", Description = "Defaults to both creation/discovery and going live. A broadcast may generate one Flight for each event type.")]
    [StaticDataSource(typeof(LiveEventDataSource))]
    public string? Events { get; set; }

    [Display("Own channel", Description = "When true, use authorized Live Streaming API for the owning channel, including accessible non-public broadcasts. Otherwise monitor public upcoming/live search results.")]
    public bool? OwnChannel { get; set; }
}
