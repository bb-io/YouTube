using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class ChannelCommentSnippetDto
{
    [JsonProperty("parentId")]
    public string ParentId { get; set; } = "";

    [JsonProperty("authorDisplayName")]
    public string AuthorDisplayName { get; set; } = "";

    [JsonProperty("textOriginal")]
    public string TextOriginal { get; set; } = "";

    [JsonProperty("textDisplay")]
    public string TextDisplay { get; set; } = "";

    [JsonProperty("publishedAt")]
    public DateTime PublishedAt { get; set; }

    [JsonProperty("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [JsonProperty("likeCount")]
    public long LikeCount { get; set; }

    [JsonProperty("moderationStatus")]
    public string? ModerationStatus { get; set; }
}
