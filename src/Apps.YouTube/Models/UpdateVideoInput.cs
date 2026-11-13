using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class UpdateVideoInput
{
    [Display("Video ID", Description = "Video ID or supported video URL. With OAuth, choose a video from the connected channel.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(VideoDataHandler))]
    public string VideoId { get; set; } = "";

    [Display("Title", Description = "Omit to preserve the source title. Maximum 100 characters.")]
    public string? Title { get; set; }

    [Display("Description", Description = "Omit to preserve the source description. An empty value clears it. Maximum 5,000 UTF-8 bytes.")]
    public string? Description { get; set; }

    [Display("Tags", Description = "Omit to preserve tags. An empty collection clears them.")]
    public IEnumerable<string>? Tags { get; set; }

    [Display("Clear tags", Description = "Set true to remove all tags. Cannot be combined with Tags.")]
    public bool? ClearTags { get; set; }

    [Display("Category ID", Description = "Omit to preserve the category.")]
    [DataSource(typeof(CategoryDataHandler))]
    public string? CategoryId { get; set; }

    [Display("Default language", Description = "Source title/description language, not a target translation language. Omit to preserve it.")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? DefaultLanguage { get; set; }

    [Display("Default audio language", Description = "Language spoken in the original audio track, not a caption language. Omit to preserve it.")]
    [DataSource(typeof(LanguageDataHandler))]
    public string? DefaultAudioLanguage { get; set; }

    [Display("Region code", Description = "Controls category suggestions only. Defaults to US.")]
    [DataSource(typeof(RegionDataHandler))]
    public string? RegionCode { get; set; }
}
