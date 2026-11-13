using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class CaptionTrack
{
    [Display("Caption ID")]
    public string CaptionId { get; set; } = "";

    [Display("Video ID")]
    public string VideoId { get; set; } = "";

    [Display("Language")]
    public string Language { get; set; } = "";

    [Display("Name")]
    public string Name { get; set; } = "";

    [Display("Is draft")]
    public bool IsDraft { get; set; }

    [Display("Track kind", Description = "ASR identifies an automatically generated track.")]
    public string TrackKind { get; set; } = "";

    [Display("Status")]
    public string Status { get; set; } = "";

    [Display("Failure reason")]
    public string FailureReason { get; set; } = "";

    [Display("Last updated")]
    public DateTime? LastUpdated { get; set; }
}
