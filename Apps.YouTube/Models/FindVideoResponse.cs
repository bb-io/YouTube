using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class FindVideoResponse
{
    [Display("Found")]
    public bool Found => Video != null;

    [Display("Video")]
    public Video? Video { get; set; }

    [Display("Video ID")]
    public string VideoId => Video?.VideoId ?? "";

    [Display("Video URL")]
    public string VideoUrl => Video?.VideoUrl ?? "";
}
