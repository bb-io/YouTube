using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class VideoLocalizationsResponse
{
    [Display("Video ID")]
    public string VideoId { get; set; } = "";

    [Display("Default language")]
    public string DefaultLanguage { get; set; } = "";

    [Display("Source title")]
    public string SourceTitle { get; set; } = "";

    [Display("Source description")]
    public string SourceDescription { get; set; } = "";

    [Display("Localizations")]
    public List<VideoLocalization> Localizations { get; set; } = [];

    [Display("Localized languages")]
    public IEnumerable<string> Languages => Localizations.Select(x => x.Language).ToArray();
}
