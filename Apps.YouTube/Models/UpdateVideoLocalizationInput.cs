using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class UpdateVideoLocalizationInput
{
    [Display("Video ID", Description = "With OAuth, choose a video from the connected channel, or enter or map Video ID directly.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(VideoDataHandler))]
    public string VideoId { get; set; } = "";

    [Display("Language", Description = "Choose the target translation language or enter/map a BCP-47 tag such as pt-BR. Regional variants remain distinct. Updates one language while preserving the others.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(LanguageDataHandler))]
    public string Language { get; set; } = "";

    [Display("Title", Description = "Required when adding a new language. Omit to preserve an existing localized title.")]
    public string? Title { get; set; }

    [Display("Description", Description = "Omit to preserve an existing description. An empty string clears the localized description.")]
    public string? Description { get; set; }

    [Display("Default language", Description = "Source metadata language. Required only when the video does not already have one. Existing source language cannot be changed by this action.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(LanguageDataHandler))]
    public string? DefaultLanguage { get; set; }
}
