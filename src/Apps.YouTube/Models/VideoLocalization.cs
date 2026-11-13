using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class VideoLocalization
{
    [Display("Language")]
    public string Language { get; set; } = "";

    [Display("Title")]
    public string Title { get; set; } = "";

    [Display("Description")]
    public string Description { get; set; } = "";
}
