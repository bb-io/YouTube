namespace Tests.YouTube;
[TestClass]
public sealed class PlaylistActionsTests : TestBase
{
    [TestMethod]
    public async Task CreatePlaylist_DefaultsToPrivate()
    {
        var playlist = await new PlaylistActions(InvocationContext).CreatePlaylist(new() { Title = "Blackbird disposable " + Guid.NewGuid().ToString("N"), Description = "Disposable test collection.", DefaultLanguage = "en" });
        TrackPlaylist(playlist.PlaylistId);
        Assert.AreEqual("private", playlist.PrivacyStatus);
        Assert.AreEqual("en", playlist.DefaultLanguage);
    }

    [TestMethod]
    public async Task GetPlaylist_ReadsPlaylistMetadata()
    {
        var id = await OwnedPlaylist();
        var result = await new PlaylistActions(InvocationContext).GetPlaylist(new() { PlaylistId = "https://www.youtube.com/playlist?list=" + id });
        Assert.AreEqual(id, result.PlaylistId);
        Assert.AreEqual("private", result.PrivacyStatus);
    }

    [TestMethod]
    public async Task SearchPlaylists_FindsOwnedPlaylist()
    {
        var id = await OwnedPlaylist();
        var result = await new PlaylistActions(InvocationContext).SearchPlaylists(new() { SearchTerm = "Blackbird disposable integration test", MaximumResults = 100 });
        Assert.IsTrue(result.Playlists.Any(x => x.PlaylistId == id));
    }

    [TestMethod]
    public async Task UpdatePlaylist_ChangesDescriptionAndPreservesTranslationsAndStatus()
    {
        var actions = new PlaylistActions(InvocationContext);
        var id = await OwnedPlaylist();
        var before = await actions.GetPlaylist(new() { PlaylistId = id });
        await new AdditionalActions(InvocationContext).ApiRequest(new() { Url = "https://www.googleapis.com/youtube/v3/playlists", Method = "PUT", QueryJson = "{\"part\":\"snippet,localizations\"}", Body = System.Text.Json.JsonSerializer.Serialize(new { id, snippet = new { title = before.Title, description = before.Description, defaultLanguage = before.DefaultLanguage }, localizations = new Dictionary<string, object> { ["fr"] = new { title = "Collection traduite", description = "Description traduite." } } }) });
        var result = await actions.UpdatePlaylist(new() { PlaylistId = id, Description = "Updated integration test description.", PrivacyStatus = "private" });
        Assert.AreEqual(before.Title, result.Title);
        Assert.AreEqual(before.PrivacyStatus, result.PrivacyStatus);
        Assert.AreEqual(before.PodcastStatus, result.PodcastStatus);
        Assert.AreEqual("en", result.DefaultLanguage);
        Assert.AreEqual("Updated integration test description.", result.Description);
        Assert.AreEqual("Collection traduite", result.Localizations.Single(x => x.Language == "fr").Title);
    }

    [TestMethod]
    public async Task SearchPlaylistVideos_PreservesRepeatedEntries()
    {
        var playlist = await OwnedPlaylist();
        var video = (await OwnedVideo()).VideoId;
        var insert = new AdditionalActions(InvocationContext);
        var first = await insert.AddVideoToPlaylist(new() { PlaylistId = playlist, VideoId = video });
        var second = await insert.AddVideoToPlaylist(new() { PlaylistId = playlist, VideoId = video });
        var actions = new PlaylistActions(InvocationContext);
        try
        {
            var result = await actions.SearchPlaylistVideos(new() { PlaylistId = playlist, VideoVisibility = "all", MaximumResults = 100 });
            var ids = result.Videos.Select(x => x.PlaylistItemId).ToList();
            Assert.IsTrue(ids.Contains(first.PlaylistItemId));
            Assert.IsTrue(ids.Contains(second.PlaylistItemId));
            Assert.IsTrue(ids.IndexOf(first.PlaylistItemId) < ids.IndexOf(second.PlaylistItemId));
        }
        finally
        {
            await actions.RemoveVideoFromPlaylist(new() { PlaylistItemId = first.PlaylistItemId, PlaylistId = playlist });
            await actions.RemoveVideoFromPlaylist(new() { PlaylistItemId = second.PlaylistItemId, PlaylistId = playlist });
        }
    }

    [TestMethod]
    public async Task RemoveVideoFromPlaylist_RemovesEntryAndPreservesVideo()
    {
        var playlist = await OwnedPlaylist();
        var video = (await OwnedVideo()).VideoId;
        var entry = await new AdditionalActions(InvocationContext).AddVideoToPlaylist(new() { PlaylistId = playlist, VideoId = video });
        var actions = new PlaylistActions(InvocationContext);
        var result = await actions.RemoveVideoFromPlaylist(new() { PlaylistItemId = entry.PlaylistItemId, PlaylistId = playlist });
        Assert.IsTrue(result.Removed);
        var items = await actions.SearchPlaylistVideos(new() { PlaylistId = playlist, VideoVisibility = "all" });
        Assert.IsFalse(items.Videos.Any(x => x.PlaylistItemId == entry.PlaylistItemId));
        var remaining = await new VideoActions(InvocationContext).GetVideoUrl(new() { VideoId = video });
        Assert.AreEqual(video, remaining.VideoId);
    }

    [TestMethod]
    public async Task SearchPlaylists_WithoutMatches_OutputsEmptyCollection()
    {
        var result = await new PlaylistActions(InvocationContext).SearchPlaylists(new() { SearchTerm = "absent-" + Guid.NewGuid().ToString("N") });
        Assert.AreEqual(0, result.Playlists.Count);
    }
}
