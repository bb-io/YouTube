namespace Tests.YouTube;
[TestClass]
public sealed class PublishingActionsTests : TestBase
{
    [TestMethod]
    public async Task UploadVideo_CreatesPrivateVideo()
    {
        var result = await OwnedVideo();
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.VideoId));
        Assert.AreEqual("private", result.PrivacyStatus);
    }

    [TestMethod]
    public async Task UpdateVideoThumbnail_SetsCustomThumbnail()
    {
        var result = await new PublishingActions(InvocationContext, FileManager).UpdateVideoThumbnail(new() { VideoId = (await OwnedVideo()).VideoId, File = new() { Name = "thumbnail.png", ContentType = "image/png" } });
        Assert.IsTrue(result.ThumbnailsJson.Contains("url"));
    }
}
