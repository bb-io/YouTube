using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class CaptionTracksResponse
{
    [Display("Caption tracks")]
    public List<CaptionTrack> CaptionTracks { get; set; } = [];

    [Display("Returned count")]
    public int ReturnedCount => CaptionTracks.Count;
}
