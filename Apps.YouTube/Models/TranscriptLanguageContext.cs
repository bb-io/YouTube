using Blackbird.Applications.Sdk.Common;

namespace Apps.YouTube;

public sealed class TranscriptLanguageContext
{
    [Display("Video ID")]
    public string? VideoId { get; set; }

    [Display("Include drafts")]
    public bool? IncludeDrafts { get; set; }

    [Display("Allow automatic captions")]
    public bool? AllowAutomaticCaptions { get; set; }
}
