using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class PickerSnippet
{
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("title")]
    public string Title { get; set; } = "";

    [JsonProperty("language")]
    public string Language { get; set; } = "";

    [JsonProperty("assignable")]
    public bool Assignable { get; set; }
}
