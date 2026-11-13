using Blackbird.Applications.Sdk.Common;

namespace Apps.YouTube;
public sealed class ChannelInput
{
    [Display("Channel URL", Description = "YouTube channel URL: https://www.youtube.com/@handle, /channel/UC..., or /user/username. For legacy /c/ links, use the channel's current @handle URL.")]
    public string ChannelUrl { get; set; } = string.Empty;

    [Display("Additional channel URLs", Description = "Optional additional YouTube channel URLs, one per list item. Watch up to 20 channels in total. New channels start with a silent baseline. The same connection and visibility filter apply to every channel.")]
    public List<string>? AdditionalChannelUrls { get; set; }

    [Display("Video visibility", Description = "Defaults to Public only. Unlisted, private, and all accessible uploads require the owning channel's OAuth connection. Changing this filter establishes a silent baseline.")]
    [Blackbird.Applications.Sdk.Common.Dictionaries.StaticDataSource(typeof(VideoVisibilityDataSource))]
    public string? VideoVisibility { get; set; }
}
