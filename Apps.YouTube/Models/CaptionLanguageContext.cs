using Blackbird.Applications.Sdk.Common;

namespace Apps.YouTube;

public sealed class CaptionLanguageContext
{
    [Display("Video ID")]
    public string? VideoId { get; set; }

    [Display("Include drafts")]
    public bool? IncludeDrafts { get; set; }
}
