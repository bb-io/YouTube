using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.YouTube;
public sealed class ConnectionValidator(InvocationContext context) : BaseInvocable(context), IConnectionValidator
{
    public async ValueTask<ConnectionValidationResponse> ValidateConnection(IEnumerable<AuthenticationCredentialsProvider> authProviders, CancellationToken cancellationToken)
    {
        try
        {
            var api = new YouTubeClient(authProviders);
            if (api.HasOAuth)
            {
                var channels = await api.GetAsync("channels", new() { ["part"] = "id", ["mine"] = "true" }, cancellationToken);
                if (YouTubeClient.Items(channels).Count == 0)
                    return new()
                    {
                        IsValid = false,
                        Message = "This Google account has no accessible YouTube channel. Reconnect using the account that manages the channel."
                    };
            }
            else
                await api.GetAsync("i18nLanguages", new() { ["part"] = "snippet" }, cancellationToken);
            return new()
            {
                IsValid = true,
                Message = "Connection successful."
            };
        }
        catch (Exception ex)
        {
            return new()
            {
                IsValid = false,
                Message = ex is Blackbird.Applications.Sdk.Common.Exceptions.PluginMisconfigurationException or Blackbird.Applications.Sdk.Common.Exceptions.PluginApplicationException ? ex.Message : "Could not validate the connection. Check the credentials, permissions and service availability."
            };
        }
    }
}
