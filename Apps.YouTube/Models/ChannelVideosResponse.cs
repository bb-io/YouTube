using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class ChannelVideosResponse
{
    [Display("Videos")]
    public List<Video> Videos { get; set; } = [];

    [Display("Video IDs")]
    public IEnumerable<string> VideoIds => Videos.Select(x => x.VideoId);

    [Display("Video URLs")]
    public IEnumerable<string> VideoUrls => Videos.Select(x => x.VideoUrl);

    [Display("Returned count")]
    public int ReturnedCount => Videos.Count;
}
