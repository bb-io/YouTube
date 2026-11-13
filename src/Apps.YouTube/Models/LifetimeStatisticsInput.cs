using Blackbird.Applications.Sdk.Common;

namespace Apps.YouTube;

public sealed class LifetimeStatisticsInput
{
    [Display("Channel URL", Description = "Omit with OAuth to use the connected channel, or choose its URL from suggestions. API-key connections require an entered or mapped public channel URL.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(ChannelUrlDataHandler))]
    public string? ChannelUrl { get; set; }
}
