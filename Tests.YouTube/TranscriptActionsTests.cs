namespace Tests.YouTube;

[TestClass]
public sealed class TranscriptActionsTests : TestBase
{
    [TestMethod]
    public async Task GetVideoTranscript_ExplicitReadyCaption_ReturnsTextSegmentsAndFile()
    {
        var track = await ReadyCaption();
        var result = await new TranscriptActions(InvocationContext, FileManager).GetVideoTranscript(new()
        {
            VideoId = track.VideoId, CaptionId = track.CaptionId, Language = track.Language, IncludeDrafts = true
        });
        Assert.IsTrue(result.TranscriptAvailable);
        Assert.AreEqual("available", result.Status);
        Assert.AreEqual(track.CaptionId, result.CaptionId);
        Assert.AreEqual("caption_id_override", result.SelectionReason);
        Assert.IsTrue(result.Text.Length > 0);
        Assert.IsTrue(result.Segments.Count > 0);
        Assert.IsTrue(result.Segments.All(segment => segment.EndSeconds > segment.StartSeconds));
        Assert.IsNotNull(result.File);
        Assert.IsTrue(result.File.Name.EndsWith(".vtt"));
        Assert.IsTrue(new FileInfo(Path.Combine(AppContext.BaseDirectory, "TestFiles", "Output", result.File.Name)).Length > 0);
    }

    [TestMethod]
    public async Task GetVideoTranscript_ExactLanguage_SelectsEligibleTrack()
    {
        var track = await ReadyCaption();
        var input = new TranscriptInput { VideoId = track.VideoId, Language = track.Language, IncludeDrafts = true };
        var languages = await new TranscriptLanguageDataHandler(InvocationContext, new() { VideoId = track.VideoId, IncludeDrafts = true })
            .GetDataAsync(new(), CancellationToken.None);
        Assert.IsTrue(languages.Any(item => item.Value.Equals(track.Language, StringComparison.OrdinalIgnoreCase)));
        var result = await new TranscriptActions(InvocationContext, FileManager).GetVideoTranscript(input);
        Assert.IsTrue(result.TranscriptAvailable);
        Assert.IsTrue(result.Language!.Equals(track.Language, StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("requested_language", result.SelectionReason);
    }

    [TestMethod]
    public async Task GetVideoTranscript_MissingExactLanguage_DoesNotSwitchLanguage()
    {
        var track = await ReadyCaption();
        var result = await new TranscriptActions(InvocationContext, FileManager).GetVideoTranscript(new()
        {
            VideoId = track.VideoId, Language = "zz-ZZ", IncludeDrafts = true
        });
        Assert.IsFalse(result.TranscriptAvailable);
        Assert.AreEqual("language_unavailable", result.Status);
        Assert.IsNull(result.File);
        Assert.AreEqual(0, result.Segments.Count);
        Assert.AreEqual("", result.Text);
    }

    private async Task<CaptionTrack> ReadyCaption()
    {
        var fixture = await OwnedCaption();
        for (var attempt = 0; attempt < 15; attempt++)
        {
            var tracks = await new LocalizationActions(InvocationContext).SearchCaptionTracks(new() { VideoId = fixture.VideoId });
            var current = tracks.CaptionTracks.SingleOrDefault(track => track.CaptionId == fixture.CaptionId);
            if (current?.Status == "serving")
                return current;
            if (current?.Status == "failed")
                Assert.Fail("Disposable caption failed processing; inspect the test fixture.");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
        Assert.Fail("Disposable caption was not ready within the bounded test wait.");
        return fixture;
    }
}
