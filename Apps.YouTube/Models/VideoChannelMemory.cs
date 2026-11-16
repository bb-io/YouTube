namespace Apps.YouTube;

public sealed class VideoChannelMemory
{
    public string ChannelKey { get; set; } = string.Empty;

    public string ChannelId { get; set; } = string.Empty;

    public string UploadsPlaylistId { get; set; } = string.Empty;

    public List<string> SeenVideoIds { get; set; } = [];

    public string VideoVisibility { get; set; } = "public";
}
