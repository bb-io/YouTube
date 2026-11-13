using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class UpdatePlaylistInput
{
    [Display("Playlist ID")]
    [DataSource(typeof(PlaylistDataHandler))]
    public string PlaylistId { get; set; } = "";

    [Display("Title", Description = "Omit to preserve the title.")]
    public string? Title { get; set; }

    [Display("Description", Description = "Omit to preserve the description. An empty value clears it.")]
    public string? Description { get; set; }

    [Display("Privacy status", Description = "Omit to preserve visibility.")]
    [StaticDataSource(typeof(PublishPrivacyDataSource))]
    public string? PrivacyStatus { get; set; }

    [Display("Default language", Description = "Omit to preserve the source language.")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? DefaultLanguage { get; set; }
}
