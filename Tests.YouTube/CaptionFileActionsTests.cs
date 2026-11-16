namespace Tests.YouTube;
[TestClass]
public sealed class CaptionFileActionsTests : TestBase
{
    [TestMethod]
    public async Task UploadCaptions_CreatesDraftTrack()
    {
        var result = await new CaptionFileActions(InvocationContext, FileManager).UploadCaptions(new() { VideoId = (await OwnedVideo()).VideoId, Language = "de", Name = "Blackbird " + Guid.NewGuid().ToString("N"), File = new() { Name = "sample.srt" }, IsDraft = true });
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.CaptionId));
        Assert.IsTrue(result.IsDraft);
    }

    [TestMethod]
    public async Task DownloadCaptions_WritesSubtitleFile()
    {
        var track = await OwnedCaption();
        var result = await new CaptionFileActions(InvocationContext, FileManager).DownloadCaptions(new() { CaptionId = track.CaptionId, Format = "srt" });
        Assert.IsTrue(new FileInfo(Path.Combine(AppContext.BaseDirectory, "TestFiles", "Output", result.File.Name)).Length > 0);
    }

    [TestMethod]
    public async Task UpdateCaptions_UpdatesExistingTrack()
    {
        var track = await OwnedCaption();
        var result = await new CaptionFileActions(InvocationContext, FileManager).UpdateCaptions(new() { VideoId = track.VideoId, CaptionId = track.CaptionId, IsDraft = true, File = new() { Name = "sample.srt" } });
        Assert.AreEqual(track.CaptionId, result.CaptionId);
        Assert.IsTrue(result.IsDraft);
    }
}
