using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Metadata;
using Blackbird.Applications.Sdk.Common.Authentication.OAuth2;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.YouTube;
public sealed class Application(InvocationContext context) : BaseInvocable(context), IApplication, ICategoryProvider
{
    public string Name => "YouTube";

    public IEnumerable<ApplicationCategory> Categories
    {
        get => [ApplicationCategory.Multimedia, ApplicationCategory.GoogleApps];
        set
        {
        }
    }

    public T GetInstance<T>()
    {
        if (typeof(T) == typeof(IOAuth2AuthorizeService) || typeof(T) == typeof(IOAuth2TokenService))
            return (T)(object)new YouTubeOAuth(InvocationContext);
        throw new NotSupportedException($"Service {typeof(T).Name} is not used by this app.");
    }
}
