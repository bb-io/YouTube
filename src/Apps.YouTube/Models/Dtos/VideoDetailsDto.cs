using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class VideoDetailsDto
{
    [JsonProperty("duration")]
    public string Duration { get; set; } = "";

    [JsonProperty("caption")]
    public string Caption { get; set; } = "";
}
