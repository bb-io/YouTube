using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.YouTube;
public sealed class UploadVideoInput
{
    [Display("Region code", Description = "Used for category suggestions only. Defaults to US; a category ID can also be entered directly.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(RegionDataHandler))]
    public string? RegionCode { get; set; }

    [Display("Title")]
    public string Title { get; set; } = "";

    [Display("Description")]
    public string Description { get; set; } = "";

    [Display("Video file")]
    public FileReference File { get; set; } = null !;

    [Display("Thumbnail", Description = "Optional. If thumbnail upload fails after video creation, the result includes a warning and the created video ID.")]
    public FileReference? Thumbnail { get; set; }

    [Display("Privacy status", Description = "Defaults to private.")]
    [StaticDataSource(typeof(PublishPrivacyDataSource))]
    public string? PrivacyStatus { get; set; }

    [Display("Publish at", Description = "Future UTC publication time; requires private visibility.")]
    public DateTime? PublishAt { get; set; }

    [Display("Tags")]
    public IEnumerable<string>? Tags { get; set; }

    [Display("Default language")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(LanguageDataHandler))]
    public string? DefaultLanguage { get; set; }

    [Display("Default audio language")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(LanguageDataHandler))]
    public string? DefaultAudioLanguage { get; set; }

    [Display("Language code", Description = "Optional target language for one localized title/description at upload. This is separate from source metadata language and original audio language; supply the localized title when adding this language.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(LanguageDataHandler))]
    public string? Language { get; set; }

    [Display("Localized title")]
    public string? LocalizedTitle { get; set; }

    [Display("Localized description")]
    public string? LocalizedDescription { get; set; }

    [Display("Recording date")]
    public DateTime? RecordingDate { get; set; }

    [Display("License")]
    [StaticDataSource(typeof(PublishLicenseDataSource))]
    public string? License { get; set; }

    [Display("Allow embedding")]
    public bool? AllowEmbedding { get; set; }

    [Display("Public statistics viewable")]
    public bool? PublicStatsViewable { get; set; }

    [Display("Category ID", Description = "Defaults to 22 (People & Blogs).")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(CategoryDataHandler))]
    public string? CategoryId { get; set; }

    [Display("Made for kids")]
    public bool? MadeForKids { get; set; }

    [Display("Notify subscribers", Description = "Defaults to true.")]
    public bool? NotifySubscribers { get; set; }

    [Display("Contains synthetic media")]
    public bool? ContainsSyntheticMedia { get; set; }
}
