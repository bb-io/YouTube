using System.Net;
using System.Text.Json;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.YouTube;
public interface IYouTubeApi
{
    bool HasOAuth { get; }

    Task VerifyOwnedChannelAsync(string channelId);
    Task<List<UploadVideo>> ListUploadsAsync(string playlistId);
    Task<ResolvedChannel> ResolveChannelAsync(ChannelReference channel);
    Task<List<string>> ListPublicUploadIdsAsync(string playlistId);
    Task<List<Video>> GetVideosAsync(IEnumerable<string> ids);
}
