using Blackbird.Applications.Sdk.Common;

namespace Apps.YouTube;
public sealed class ChannelVideoComment
{
    [Display("Channel ID")]
    public string ChannelId { get; set; } = "";

    [Display("Video ID")]
    public string VideoId { get; set; } = "";

    [Display("Comment ID")]
    public string CommentId { get; set; } = "";

    [Display("Parent comment ID")]
    public string ParentCommentId { get; set; } = "";

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

    [Display("Channel URL")]
    public string ChannelUrl => "https://www.youtube.com/channel/" + ChannelId;

    [Display("Video URL")]
    public string VideoUrl => "https://www.youtube.com/watch?v=" + VideoId;

    [Display("Comment URL")]
    public string CommentUrl => VideoUrl + "&lc=" + Uri.EscapeDataString(CommentId);

    [Display("Is reply")]
    public bool IsReply => !string.IsNullOrEmpty(ParentCommentId);
}
