using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class CreatePlaylistInput
{
    [Display("Title", Description = "Name for the new playlist.")]
    public string Title { get; set; } = "";

    [Display("Description")]
    public string? Description { get; set; }

    [Display("Privacy status", Description = "Defaults to private.")]
    [StaticDataSource(typeof(PublishPrivacyDataSource))]
    public string? PrivacyStatus { get; set; }

    [Display("Default language")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? DefaultLanguage { get; set; }
}
