using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class DownloadCaptionsInput
{
    [Display("Video ID", Description = "Optional. Select or map Video ID before opening Caption ID suggestions. A directly entered/mapped Caption ID still works without Video ID. Clear any previous caption selection after changing the video.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(VideoDataHandler))]
    public string? VideoId { get; set; }

    [Display("Caption ID")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(CaptionDataHandler))]
    public string CaptionId { get; set; } = "";

    [Display("Format", Description = "Defaults to SRT. YouTube performs the format conversion.")]
    [StaticDataSource(typeof(CaptionFormatDataSource))]
    public string? Format { get; set; }
}
