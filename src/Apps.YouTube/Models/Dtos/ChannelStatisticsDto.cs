using Newtonsoft.Json;

namespace Apps.YouTube;

public sealed class ChannelStatisticsDto
{
    [JsonProperty("viewCount")]
    public long? ViewCount { get; set; }

    [JsonProperty("subscriberCount")]
    public long? SubscriberCount { get; set; }

    [JsonProperty("hiddenSubscriberCount")]
    public bool HiddenSubscriberCount { get; set; }

    [JsonProperty("videoCount")]
    public long? VideoCount { get; set; }
}
