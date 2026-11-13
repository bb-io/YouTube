using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class ChannelCommentChannelDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = "";
}
