using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class ThumbnailResponse
{
    [Display("Thumbnails JSON")]
    public string ThumbnailsJson { get; set; } = "";
}
