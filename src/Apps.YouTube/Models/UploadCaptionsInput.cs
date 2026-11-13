using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class UploadCaptionsInput
{
    [Display("Video ID", Description = "With OAuth, choose a video from the connected channel, or enter or map Video ID directly.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(VideoDataHandler))]
    public string VideoId { get; set; } = "";

    [Display("Language")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(LanguageDataHandler))]
    public string Language { get; set; } = "";

    [Display("Name", Description = "Track name, up to 150 characters. Defaults to the language code.")]
    public string? Name { get; set; }

    [Display("File", Description = "A time-coded subtitle file accepted by YouTube, such as SRT or VTT. Maximum 100 MB. Auto-sync is not used.")]
    public FileReference File { get; set; } = null !;

    [Display("Is draft", Description = "Defaults to true, so a new track can be reviewed before publication.")]
    public bool? IsDraft { get; set; }
}
