using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Tests.YouTube;
[TestClass]
public sealed class HandlerTests : TestBase
{
    [TestMethod]
    public async Task VideoDataHandler_FindsOwnedVideoById()
    {
        var id = (await OwnedVideo()).VideoId;
        var expected = await new VideoActions(InvocationContext).GetVideoUrl(new() { VideoId = id });
        var result = (await new VideoDataHandler(InvocationContext).GetDataAsync(new() { SearchString = id }, CancellationToken.None)).ToList();
        var item = result.Single(value => value.Value == id);
        Assert.IsTrue(item.DisplayName.Contains(expected.Title));
        Assert.IsTrue(item.DisplayName.Contains(expected.PrivacyStatus));
        Assert.IsTrue(item.DisplayName.Contains(id));
    }

    [TestMethod]
    public async Task VideoDataHandler_SearchesCurrentOwnedTitle()
    {
        var id = (await OwnedVideo()).VideoId;
        var expected = await new VideoActions(InvocationContext).GetVideoUrl(new() { VideoId = id });
        var query = expected.Title.ToUpperInvariant();
        var result = (await new VideoDataHandler(InvocationContext).GetDataAsync(new() { SearchString = query }, CancellationToken.None)).ToList();
        Assert.IsTrue(result.Any(value => value.Value == id));
        Assert.IsTrue(result.Count <= 50);
        Assert.IsTrue(result.All(value => value.DisplayName.Contains(expected.Title, StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task PlaylistDataHandler_FindsOwnedPlaylistById()
    {
        var id = await OwnedPlaylist();
        var result = (await new PlaylistDataHandler(InvocationContext).GetDataAsync(new() { SearchString = id }, CancellationToken.None)).ToList();
        Assert.IsTrue(result.Any(value => value.Value == id && value.DisplayName.Contains(id)));
    }

    [TestMethod]
    public async Task LanguageDataHandler_FiltersLanguages()
    {
        var result = (await new LanguageDataHandler(InvocationContext).GetDataAsync(new() { SearchString = "English" }, CancellationToken.None)).ToList();
        Assert.IsTrue(result.Any(x => x.Value == "en"));
    }

    [TestMethod]
    public async Task RegionDataHandler_FiltersRegions()
    {
        var result = (await new RegionDataHandler(InvocationContext).GetDataAsync(new() { SearchString = "US" }, CancellationToken.None)).ToList();
        Assert.IsTrue(result.Any(x => x.Value == "US"));
    }

    [TestMethod]
    public async Task CategoryDataHandler_ReadsRegionalCategories()
    {
        var result = await new CategoryDataHandler(InvocationContext, new() { RegionCode = "US" }).GetDataAsync(new(), CancellationToken.None);
        Assert.IsTrue(result.Any(x => x.Value == "22"));
    }

    [TestMethod]
    public async Task PlaylistDataHandler_ReadsOwnedPlaylists()
    {
        await OwnedPlaylist();
        var result = (await new PlaylistDataHandler(InvocationContext).GetDataAsync(new(), CancellationToken.None)).ToList();
        Assert.IsTrue(result.Count > 0);
        Assert.IsTrue(result.Count <= 50);
    }

    [TestMethod]
    public async Task CaptionDataHandler_ReadsVideoTracks()
    {
        var track = await OwnedCaption();
        var result = await new CaptionDataHandler(InvocationContext, new() { VideoId = track.VideoId }).GetDataAsync(new(), CancellationToken.None);
        var item = result.Single(x => x.Value == track.CaptionId);
        Assert.IsTrue(item.DisplayName.Contains(track.Language));
        Assert.IsTrue(item.DisplayName.Contains(track.CaptionId));
        Assert.IsTrue(item.DisplayName.Contains("Draft") || item.DisplayName.Contains("Not draft"));
    }

    [TestMethod]
    public async Task PlaylistItemDataHandler_ReadsSelectedPlaylist()
    {
        var playlist = await OwnedPlaylist();
        var entry = await new AdditionalActions(InvocationContext).AddVideoToPlaylist(new() { PlaylistId = playlist, VideoId = (await OwnedVideo()).VideoId });
        try
        {
            var result = await new PlaylistItemDataHandler(InvocationContext, new() { PlaylistId = playlist }).GetDataAsync(new() { SearchString = entry.VideoId }, CancellationToken.None);
            Assert.IsTrue(result.Any(x => x.Value == entry.PlaylistItemId));
        }
        finally
        {
            await new PlaylistActions(InvocationContext).RemoveVideoFromPlaylist(new() { PlaylistItemId = entry.PlaylistItemId, PlaylistId = playlist });
        }
    }
}
