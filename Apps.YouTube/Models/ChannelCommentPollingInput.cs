using Blackbird.Applications.Sdk.Common;

namespace Apps.YouTube;
public sealed class ChannelCommentPollingInput
{
    [Display("Channel URL", Description = "Channel URL using its @handle, /channel/UC... ID or /user/username. Monitors accessible published comments under the channel's videos.")]
    public string ChannelUrl { get; set; } = "";

    [Display("Include replies", Description = "Defaults to true. Includes new replies on older threads; full reply enumeration can consume substantial API quota.")]
    public bool? IncludeReplies { get; set; }
}
