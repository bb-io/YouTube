using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.YouTube;

public sealed class TranscriptResponse
{
    [Display("Video ID")]
    public string VideoId { get; set; } = "";

    [Display("Video URL")]
    public string VideoUrl { get; set; } = "";

    [Display("Title")]
    public string Title { get; set; } = "";

    [Display("Transcript available")]
    public bool TranscriptAvailable { get; set; }

    [Display("Status", Description = "Machine-readable availability status; permission/download/parser failures are errors.")]
    public string Status { get; set; } = "";

    [Display("Status message")]
    public string StatusMessage { get; set; } = "";

    [Display("Retry recommended", Description = "A later attempt may help for missing/processing captions; no retry is performed automatically.")]
    public bool RetryRecommended { get; set; }

    [Display("Transcript text", Description = "Text from the selected caption track. Repeated/overlapping cues are preserved; no AI rewriting.")]
    public string Text { get; set; } = "";

    [Display("Segments", Description = "Timed cue text in source order.")]
    public List<TranscriptSegment> Segments { get; set; } = [];

    [Display("Subtitle file", Description = "Downloaded WebVTT file, present only when a non-empty transcript is returned.")]
    public FileReference? File { get; set; }

    [Display("Caption ID")]
    public string? CaptionId { get; set; }

    [Display("Language", Description = "Actual selected track language; empty when no track is selected.")]
    public string? Language { get; set; }

    [Display("Requested language")]
    public string? RequestedLanguage { get; set; }

    [Display("Selection reason", Description = "How the language/track was selected.")]
    public string SelectionReason { get; set; } = "";

    [Display("Track kind", Description = "standard, ASR or forced as reported by YouTube. Standard is not a human-authorship guarantee.")]
    public string? TrackKind { get; set; }

    [Display("Track name")]
    public string? TrackName { get; set; }

    [Display("Is draft")]
    public bool? IsDraft { get; set; }

    [Display("Is automatic", Description = "True for YouTube ASR track kind. False does not prove human authorship.")]
    public bool? IsAutomatic { get; set; }

    [Display("Is forced")]
    public bool? IsForced { get; set; }

    [Display("Available languages", Description = "All accessible caption language tags, including tracks excluded by selection settings.")]
    public List<string> AvailableLanguages { get; set; } = [];

    [Display("Warnings", Description = "Track provenance or coverage notes. Caption completeness is not verified against audio.")]
    public List<string> Warnings { get; set; } = [];
}
