using Blackbird.Applications.Sdk.Common;

namespace Apps.YouTube;

public sealed class TranscriptInput
{
    [Display("Video ID", Description = "Choose, enter or map a video ID or supported URL. Requires OAuth and permission to edit the video.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(VideoDataHandler))]
    public string VideoId { get; set; } = "";

    [Display("Language", Description = "Optional exact caption language. Otherwise use declared audio language, then metadata language; without either, select only a sole eligible language. No regional fallback.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(TranscriptLanguageDataHandler))]
    public string? Language { get; set; }

    [Display("Caption ID", Description = "Optional advanced override for a specific track on this video. Language and draft/automatic settings must agree. Forced tracks are allowed only through an explicit override.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(CaptionDataHandler))]
    public string? CaptionId { get; set; }

    [Display("Include drafts", Description = "Defaults to false. Explicitly enable to allow draft tracks, including an overridden Caption ID.")]
    public bool? IncludeDrafts { get; set; }

    [Display("Allow automatic captions", Description = "Defaults to false. Explicitly enable ASR tracks; the action does not generate speech recognition or correct caption text.")]
    public bool? AllowAutomaticCaptions { get; set; }
}
