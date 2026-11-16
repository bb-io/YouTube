using Blackbird.Applications.Sdk.Common;

namespace Apps.YouTube;
public sealed class VideoInput
{
    [Display("Video ID", Description = "Map Video ID from the event, or enter an 11-character video ID. YouTube watch, Shorts and youtu.be URLs are also accepted. With OAuth, choose a video from the connected channel.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(VideoDataHandler))]
    public string VideoId { get; set; } = string.Empty;
}
