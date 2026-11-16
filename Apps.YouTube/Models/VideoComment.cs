using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class VideoComment
{
    [Display("Comment ID")]
    public string CommentId { get; set; } = "";

    [Display("Parent comment ID")]
    public string ParentCommentId { get; set; } = "";

    [Display("Video ID")]
    public string VideoId { get; set; } = "";

    [Display("Author")]
    public string Author { get; set; } = "";

    [Display("Text")]
    public string Text { get; set; } = "";

    [Display("Published at")]
    public DateTime PublishedAt { get; set; }

    [Display("Updated at")]
    public DateTime? UpdatedAt { get; set; }

    [Display("Like count")]
    public long LikeCount { get; set; }
}
