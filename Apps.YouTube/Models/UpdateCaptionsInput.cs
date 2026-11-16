using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class UpdateCaptionsInput
{
    [Display("Video ID", Description = "Select or map Video ID before Caption ID. The action verifies the caption belongs to this video. Clear a previous caption selection after changing the video.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(VideoDataHandler))]
    public string VideoId { get; set; } = "";

    [Display("Caption ID")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(CaptionDataHandler))]
    public string CaptionId { get; set; } = "";

    [Display("File", Description = "Optional replacement time-coded subtitle file. Omit to update only draft status.")]
    public FileReference? File { get; set; }

    [Display("Is draft", Description = "Omit to preserve the current draft status. Set false to publish the track.")]
    public bool? IsDraft { get; set; }
}
