using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class MissingLanguagesResponse
{
    [Display("Missing metadata languages")]
    public List<string> MissingMetadataLanguages { get; set; } = [];

    [Display("Missing caption languages")]
    public List<string> MissingCaptionLanguages { get; set; } = [];

    [Display("Available metadata languages")]
    public List<string> AvailableMetadataLanguages { get; set; } = [];

    [Display("Available caption languages")]
    public List<string> AvailableCaptionLanguages { get; set; } = [];
}
