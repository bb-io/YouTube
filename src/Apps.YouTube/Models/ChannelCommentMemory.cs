namespace Apps.YouTube;
public sealed class ChannelCommentMemory
{
    public string ChannelId { get; set; } = "";
    public bool IncludeReplies { get; set; } = true;
    public List<string> SeenCommentIds { get; set; } = [];
}
