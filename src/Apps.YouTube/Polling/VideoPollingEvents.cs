using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common.Polling;

namespace Apps.YouTube;

[PollingEventList]
public sealed class VideoPollingEvents(InvocationContext context) : BaseInvocable(context)
{
    [PollingEvent("On new video added", "Start a workflow when a new video in any watched channel matches the selected visibility. Public videos are selected by default; other modes require the owner’s OAuth connection.")]
    [MultipleEvents]
    public Task<PollingEventResponse<PollingMemory, List<Video>>> OnNewVideoAdded(
        PollingEventRequest<PollingMemory> request,
        [PollingEventParameter] ChannelInput input) =>
        YouTubeClient.RunAsync<PollingEventResponse<PollingMemory, List<Video>>>(() =>
            new VideoPoller(new VideoActions(new YouTubeClient(InvocationContext.AuthenticationCredentialsProviders)))
                .PollAsync(request.Memory, input.ChannelUrl, input.VideoVisibility, input.AdditionalChannelUrls));
}
