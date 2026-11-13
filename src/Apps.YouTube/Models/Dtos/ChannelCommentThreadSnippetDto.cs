using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class ChannelCommentThreadSnippetDto
{
    [JsonProperty("videoId")]
    public string VideoId { get; set; } = "";

    [JsonProperty("totalReplyCount")]
    public long TotalReplyCount { get; set; }

    [JsonProperty("topLevelComment")]
    public ChannelCommentDto TopLevelComment { get; set; } = new();
}
