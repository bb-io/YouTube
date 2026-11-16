using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class ChannelCommentDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = "";

    [JsonProperty("snippet")]
    public ChannelCommentSnippetDto Snippet { get; set; } = new();
}
