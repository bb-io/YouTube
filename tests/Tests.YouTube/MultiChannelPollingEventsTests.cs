using System.Text.Json;
using Blackbird.Applications.Sdk.Common.Polling;

namespace Tests.YouTube;

[TestClass]
public sealed class MultiChannelPollingEventsTests : TestBase
{
    [TestMethod]
    public async Task OnNewVideoAdded_MultiplePublicChannels_BaselinesAndRestoresMemory()
    {
        var urls = await PublicChannelUrls();
        var input = new ChannelInput
        {
            ChannelUrl = urls[0],
            AdditionalChannelUrls = [urls[1]]
        };
        var events = new VideoPollingEvents(InvocationContext);
        var first = await events.OnNewVideoAdded(new(), input);
        Assert.IsFalse(first.FlyBird);
        Assert.AreEqual(2, first.Memory!.Channels.Count);
        var restored = JsonSerializer.Deserialize<PollingMemory>(JsonSerializer.Serialize(first.Memory));
        var second = await events.OnNewVideoAdded(new() { Memory = restored }, input);
        Assert.AreEqual(2, second.Memory!.Channels.Count);
        if (second.FlyBird)
        {
            Assert.IsTrue(second.Result!.Count <= 100);
            Assert.IsTrue(second.Result.All(video => video.PrivacyStatus == "public"));
            Assert.IsTrue(second.Result.All(video => first.Memory.Channels.Any(channel => channel.ChannelId == video.ChannelId)));
            Assert.IsTrue(second.Result.All(video => !first.Memory.Channels.Single(channel => channel.ChannelId == video.ChannelId).SeenVideoIds.Contains(video.VideoId)));
        }
    }

    [TestMethod]
    public async Task OnNewVideoAdded_AdditionalChannel_BaselinesOnlyNewChannel()
    {
        var urls = await PublicChannelUrls();
        var events = new VideoPollingEvents(InvocationContext);
        var first = await events.OnNewVideoAdded(new(), new() { ChannelUrl = urls[0] });
        var added = await events.OnNewVideoAdded(new() { Memory = first.Memory }, new()
        {
            ChannelUrl = urls[0],
            AdditionalChannelUrls = [urls[1], urls[0], urls[1] + "/videos"]
        });
        Assert.AreEqual(2, added.Memory!.Channels.Count);
        Assert.IsTrue(first.Memory!.SeenVideoIds.All(added.Memory.Channels[0].SeenVideoIds.Contains));
        if (added.FlyBird)
            Assert.IsTrue(added.Result!.All(video => video.ChannelId == first.Memory.ChannelId));
        var reordered = await events.OnNewVideoAdded(new() { Memory = added.Memory }, new()
        {
            ChannelUrl = urls[1],
            AdditionalChannelUrls = [urls[0]]
        });
        Assert.AreEqual(2, reordered.Memory!.Channels.Count);
        Assert.IsTrue(added.Memory.Channels.All(old => old.SeenVideoIds.All(reordered.Memory.Channels.Single(current => current.ChannelId == old.ChannelId).SeenVideoIds.Contains)));
    }

    [TestMethod]
    public async Task OnNewVideoAdded_LegacyOwnerMemory_PreservesHistory()
    {
        var id = await OwnedChannelId();
        var input = new ChannelInput
        {
            ChannelUrl = "https://www.youtube.com/channel/" + id,
            VideoVisibility = "all"
        };
        var events = new VideoPollingEvents(InvocationContext);
        var first = await events.OnNewVideoAdded(new(), input);
        var legacy = new PollingMemory
        {
            ChannelKey = first.Memory!.ChannelKey,
            ChannelId = first.Memory.ChannelId,
            UploadsPlaylistId = first.Memory.UploadsPlaylistId,
            SeenVideoIds = first.Memory.SeenVideoIds.ToList(),
            VideoVisibility = first.Memory.VideoVisibility
        };
        var upgraded = await events.OnNewVideoAdded(new() { Memory = legacy }, input);
        Assert.AreEqual(1, upgraded.Memory!.Channels.Count);
        Assert.IsTrue(legacy.SeenVideoIds.All(upgraded.Memory.Channels[0].SeenVideoIds.Contains));
        if (upgraded.FlyBird)
            Assert.IsTrue(upgraded.Result!.All(video => !legacy.SeenVideoIds.Contains(video.VideoId)));
    }

    private async Task<List<string>> PublicChannelUrls()
    {
        var search = await new VideoActions(InvocationContext).SearchVideos(new()
        {
            SearchTerm = "localization translation",
            MaximumResults = 25
        });
        var ids = search.Videos.Select(video => video.ChannelId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().Take(2).ToList();
        Assert.AreEqual(2, ids.Count, "Provide public search access to at least two channels for this integration test.");
        return ids.Select(id => "https://www.youtube.com/channel/" + id).ToList();
    }
}
