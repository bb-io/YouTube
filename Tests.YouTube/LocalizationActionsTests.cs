namespace Tests.YouTube;
[TestClass]
public sealed class LocalizationActionsTests : TestBase
{
    [TestMethod]
    public async Task GetVideoLocalizations_ReadsOwnedVideo()
    {
        var video = await OwnedVideo();
        var result = await new LocalizationActions(InvocationContext).GetVideoLocalizations(new() { VideoId = video.VideoId });
        Assert.AreEqual(video.VideoId, result.VideoId);
        Assert.AreEqual("en", result.DefaultLanguage);
    }

    [TestMethod]
    public async Task UpdateVideoLocalization_PreservesSource()
    {
        var result = await new LocalizationActions(InvocationContext).UpdateVideoLocalization(new() { VideoId = (await OwnedVideo()).VideoId, Language = "fr", Title = "Vidéo de test", Description = "Description de test." });
        Assert.AreEqual("Vidéo de test", result.Localizations.Single(x => x.Language == "fr").Title);
        Assert.AreEqual("en", result.DefaultLanguage);
    }

    [TestMethod]
    public async Task SearchCaptionTracks_FindsCreatedTrack()
    {
        var track = await OwnedCaption();
        var result = await new LocalizationActions(InvocationContext).SearchCaptionTracks(new() { VideoId = track.VideoId });
        Assert.IsTrue(result.CaptionTracks.Any(c => c.CaptionId == track.CaptionId));
    }

    [TestMethod]
    public async Task FindMissingLanguages_IdentifiesAbsentLanguage()
    {
        var result = await new LocalizationActions(InvocationContext).FindMissingLanguages(new() { VideoId = (await OwnedVideo()).VideoId, RequiredLanguages = ["ja"] });
        CollectionAssert.Contains(result.MissingMetadataLanguages, "ja");
        CollectionAssert.Contains(result.MissingCaptionLanguages, "ja");
    }
}
