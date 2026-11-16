using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class CaptionFileResponse
{
    [Display("File")]
    public FileReference File { get; set; } = null !;

    [Display("Caption ID")]
    public string CaptionId { get; set; } = "";
}
