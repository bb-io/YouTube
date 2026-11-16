using Blackbird.Applications.Sdk.Common;

namespace Apps.YouTube;

public sealed class TranscriptSegment
{
    [Display("Start in seconds", Description = "Caption cue start time relative to the downloaded caption track.")]
    public double StartSeconds { get; set; }

    [Display("End in seconds", Description = "Caption cue end time. Overlapping cues are retained.")]
    public double EndSeconds { get; set; }

    [Display("Text", Description = "Caption text with formatting markup removed.")]
    public string Text { get; set; } = "";
}
