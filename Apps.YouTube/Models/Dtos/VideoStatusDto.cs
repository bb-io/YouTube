using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class VideoStatusDto
{
    [JsonProperty("privacyStatus")]
    public string PrivacyStatus { get; set; } = "";

    [JsonProperty("uploadStatus")]
    public string UploadStatus { get; set; } = "";

    [JsonProperty("publishAt")]
    public DateTime? PublishAt { get; set; }

    [JsonProperty("license")]
    public string? License { get; set; }

    [JsonProperty("embeddable")]
    public bool? Embeddable { get; set; }

    [JsonProperty("publicStatsViewable")]
    public bool? PublicStatsViewable { get; set; }

    [JsonProperty("selfDeclaredMadeForKids")]
    public bool? SelfDeclaredMadeForKids { get; set; }

    [JsonProperty("containsSyntheticMedia")]
    public bool? ContainsSyntheticMedia { get; set; }
}
