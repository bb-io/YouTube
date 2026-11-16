using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class ChannelCommentThreadDto
{
    [JsonProperty("id")]
    public string Id { get; set; } = "";

    [JsonProperty("snippet")]
    public ChannelCommentThreadSnippetDto Snippet { get; set; } = new();
}
