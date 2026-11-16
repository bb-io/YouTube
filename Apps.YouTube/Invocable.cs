using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.YouTube;
public class Invocable : BaseInvocable
{
    public Invocable(InvocationContext context) : base(context)
    {
        Client = new(context.AuthenticationCredentialsProviders);
    }

    internal Invocable(YouTubeClient client) : base(new InvocationContext())
    {
        Client = client;
    }

    protected YouTubeClient Client { get; }
}
