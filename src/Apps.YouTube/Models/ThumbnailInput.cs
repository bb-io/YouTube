using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class ThumbnailInput
{
    [Display("Video ID", Description = "With OAuth, choose a video from the connected channel, or enter or map Video ID directly.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(VideoDataHandler))]
    public string VideoId { get; set; } = "";

    [Display("Thumbnail")]
    public FileReference File { get; set; } = null !;
}
