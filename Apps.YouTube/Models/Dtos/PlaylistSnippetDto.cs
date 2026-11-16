using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class PlaylistSnippetDto
{
    [JsonProperty("title")]
    public string Title { get; set; } = "";

    [JsonProperty("description")]
    public string Description { get; set; } = "";

    [JsonProperty("channelId")]
    public string ChannelId { get; set; } = "";

    [JsonProperty("defaultLanguage")]
    public string DefaultLanguage { get; set; } = "";

    [JsonProperty("publishedAt")]
    public DateTime PublishedAt { get; set; }
}
