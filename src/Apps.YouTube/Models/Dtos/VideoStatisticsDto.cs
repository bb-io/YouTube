using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class VideoStatisticsDto
{
    [JsonProperty("viewCount")]
    public long? ViewCount { get; set; }
    [JsonProperty("likeCount")]
    public long? LikeCount { get; set; }

    [JsonProperty("commentCount")]
    public long? CommentCount { get; set; }
}
