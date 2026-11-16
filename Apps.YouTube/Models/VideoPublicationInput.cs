using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Newtonsoft.Json;

namespace Apps.YouTube;
public sealed class VideoPublicationInput
{
    [Display("Video ID", Description = "With OAuth, choose a video from the connected channel, or enter or map Video ID directly.")]
    [Blackbird.Applications.Sdk.Common.Dynamic.DataSource(typeof(VideoDataHandler))]
    public string VideoId { get; set; } = "";

    [Display("Privacy status", Description = "Omit to preserve visibility; nothing is inferred from the file or language. Public or unlisted clears an existing publication schedule.")]
    [StaticDataSource(typeof(PublishPrivacyDataSource))]
    public string? PrivacyStatus { get; set; }

    [Display("Publish at", Description = "Future UTC time for an already private, never-published video. Use private target visibility. Do not combine with Cancel scheduled publication.")]
    public DateTime? PublishAt { get; set; }

    [Display("Cancel scheduled publication", Description = "Set true to remove the schedule. Cannot be combined with Publish at.")]
    public bool? CancelScheduledPublication { get; set; }

    [Display("License", Description = "Omit to preserve the license.")]
    [StaticDataSource(typeof(PublishLicenseDataSource))]
    public string? License { get; set; }

    [Display("Allow embedding")]
    public bool? AllowEmbedding { get; set; }

    [Display("Public statistics viewable")]
    public bool? PublicStatsViewable { get; set; }

    [Display("Made for kids")]
    public bool? MadeForKids { get; set; }

    [Display("Contains synthetic media")]
    public bool? ContainsSyntheticMedia { get; set; }
}
