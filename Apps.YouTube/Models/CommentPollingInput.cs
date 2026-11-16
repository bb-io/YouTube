using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class CommentPollingInput
{
    [Display("Video ID")]
    public string VideoId { get; set; } = "";

    [Display("Include replies", Description = "Defaults to true. Full reply pagination adds API calls for threads with replies.")]
    public bool? IncludeReplies { get; set; }
}
