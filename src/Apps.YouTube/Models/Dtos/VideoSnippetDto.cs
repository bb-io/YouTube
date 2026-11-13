using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class VideoSnippetDto
{
    [JsonProperty("title")]
    public string Title { get; set; } = "";

    [JsonProperty("description")]
    public string Description { get; set; } = "";

    [JsonProperty("channelId")]
    public string ChannelId { get; set; } = "";

    [JsonProperty("channelTitle")]
    public string ChannelTitle { get; set; } = "";

    [JsonProperty("categoryId")]
    public string CategoryId { get; set; } = "";

    [JsonProperty("defaultLanguage")]
    public string DefaultLanguage { get; set; } = "";

    [JsonProperty("defaultAudioLanguage")]
    public string DefaultAudioLanguage { get; set; } = "";

    [JsonProperty("publishedAt")]
    public DateTime PublishedAt { get; set; }

    [JsonProperty("tags")]
    public List<string>? Tags { get; set; }
}
