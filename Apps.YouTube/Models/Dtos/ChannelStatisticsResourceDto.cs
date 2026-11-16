using Newtonsoft.Json;

namespace Apps.YouTube;

public sealed class ChannelStatisticsResourceDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = "";

    [JsonProperty("snippet")]
    public PickerSnippet Snippet { get; set; } = new();

    [JsonProperty("statistics")]
    public ChannelStatisticsDto Statistics { get; set; } = new();
}
