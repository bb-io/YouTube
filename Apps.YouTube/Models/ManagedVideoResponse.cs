using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class ManagedVideoResponse
{
    [Display("Video ID")]
    public string VideoId { get; set; } = "";

    [Display("Video URL")]
    public string VideoUrl => "https://www.youtube.com/watch?v=" + VideoId;

    [Display("Title")]
    public string Title { get; set; } = "";

    [Display("Description")]
    public string Description { get; set; } = "";

    [Display("Category ID")]
    public string CategoryId { get; set; } = "";

    [Display("Default language")]
    public string DefaultLanguage { get; set; } = "";

    [Display("Default audio language")]
    public string DefaultAudioLanguage { get; set; } = "";

    [Display("Privacy status")]
    public string PrivacyStatus { get; set; } = "";

    [Display("Upload status")]
    public string UploadStatus { get; set; } = "";

    [Display("Tags")]
    public List<string> Tags { get; set; } = [];

    [Display("Publish at")]
    public DateTime? PublishAt { get; set; }

    [Display("License")]
    public string? License { get; set; }

    [Display("Allow embedding")]
    public bool? AllowEmbedding { get; set; }

    [Display("Public statistics viewable")]
    public bool? PublicStatsViewable { get; set; }

    [Display("Made for kids")]
    public bool? MadeForKids { get; set; }

    [Display("Contains synthetic media")]
    public bool? ContainsSyntheticMedia { get; set; }
}
