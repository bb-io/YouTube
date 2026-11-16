using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Tests.YouTube;
[TestClass]
public sealed class VideoManagementActionsTests : TestBase
{
    [TestMethod]
    public async Task UpdateVideo_ChangesTitleAndPreservesOtherFields()
    {
        var id = (await OwnedVideo()).VideoId;
        var localization = new LocalizationActions(InvocationContext);
        await localization.UpdateVideoLocalization(new() { VideoId = id, Language = "fr", Title = "Titre conservé", Description = "Description conservée." });
        var before = await localization.GetVideoLocalizations(new() { VideoId = id });
        var result = await new VideoManagementActions(InvocationContext).UpdateVideo(new() { VideoId = id, Title = "Blackbird updated " + Guid.NewGuid().ToString("N") });
        var after = await localization.GetVideoLocalizations(new() { VideoId = id });
        Assert.AreEqual(before.SourceDescription, result.Description);
        Assert.AreEqual("private", result.PrivacyStatus);
        Assert.AreEqual("en", result.DefaultLanguage);
        CollectionAssert.AreEquivalent(new List<string> { "blackbird", "integration" }, result.Tags);
        Assert.AreEqual("Titre conservé", after.Localizations.Single(x => x.Language == "fr").Title);
    }

    [TestMethod]
    public async Task SetVideoPublicationSettings_SchedulesPreservesAndCancels()
    {
        var id = (await OwnedVideo()).VideoId;
        var actions = new VideoManagementActions(InvocationContext);
        var time = DateTime.UtcNow.AddDays(2);
        try
        {
            var scheduled = await actions.SetVideoPublicationSettings(new() { VideoId = id, PrivacyStatus = "private", PublishAt = time, AllowEmbedding = false, MadeForKids = false });
            Assert.AreEqual("private", scheduled.PrivacyStatus);
            Assert.IsTrue(scheduled.PublishAt.HasValue && Math.Abs((scheduled.PublishAt.Value - time).TotalSeconds) < 2);
            var updated = await actions.SetVideoPublicationSettings(new() { VideoId = id, ContainsSyntheticMedia = true });
            Assert.AreEqual(false, updated.AllowEmbedding);
            Assert.AreEqual(true, updated.ContainsSyntheticMedia);
            Assert.AreEqual(scheduled.PublishAt, updated.PublishAt);
        }
        finally
        {
            var canceled = await actions.SetVideoPublicationSettings(new() { VideoId = id, CancelScheduledPublication = true, PrivacyStatus = "private", AllowEmbedding = true, ContainsSyntheticMedia = false });
            Assert.IsNull(canceled.PublishAt);
        }
    }

    [TestMethod]
    public async Task SearchChannelVideos_FindsPrivateOwnedVideo()
    {
        var id = (await OwnedVideo()).VideoId;
        var result = await new VideoManagementActions(InvocationContext).SearchChannelVideos(new() { VideoVisibility = "private", SearchTerm = "Disposable integration test video.", MaximumResults = 100 });
        Assert.IsTrue(result.Videos.Any(x => x.VideoId == id));
        Assert.IsTrue(result.Videos.All(x => x.PrivacyStatus == "private"));
    }

    [TestMethod]
    public async Task SearchChannelVideos_WithoutMatches_OutputsEmptyCollection()
    {
        var result = await new VideoManagementActions(InvocationContext).SearchChannelVideos(new() { SearchTerm = "absent-" + Guid.NewGuid().ToString("N"), MaximumResults = 1 });
        Assert.AreEqual(0, result.Videos.Count);
    }

    [TestMethod]
    public async Task UpdateVideo_ClearTags_ExplicitlyRemovesAndRestoresTags()
    {
        var id = (await OwnedVideo()).VideoId;
        var actions = new VideoManagementActions(InvocationContext);
        try
        {
            var result = await actions.UpdateVideo(new() { VideoId = id, ClearTags = true });
            Assert.AreEqual(0, result.Tags.Count);
        }
        finally
        {
            await actions.UpdateVideo(new() { VideoId = id, Tags = ["blackbird", "integration"] });
        }
    }

    [TestMethod]
    public async Task SetVideoPublicationSettings_RejectsPastSchedule()
    {
        var id = (await OwnedVideo()).VideoId;
        await Assert.ThrowsExceptionAsync<PluginMisconfigurationException>(() => new VideoManagementActions(InvocationContext).SetVideoPublicationSettings(new() { VideoId = id, PublishAt = DateTime.UtcNow.AddDays(-1), PrivacyStatus = "private" }));
    }
}
