using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.YouTube;
public sealed class ConnectionDefinition : IConnectionDefinition
{
    public IEnumerable<ConnectionPropertyGroup> ConnectionPropertyGroups => [new()
    {
        Name = "YouTube Data API key",
        AuthenticationType = ConnectionAuthenticationType.Undefined,
        ConnectionProperties = [new("api_key")
        {
            DisplayName = "API key",
            Sensitive = true,
            Description = "Google Cloud API key from a project with YouTube Data API v3 enabled. Used to read public channels and videos."
        }

        ]
    }, new()
    {
        Name = "YouTube channel management (OAuth)",
        AuthenticationType = ConnectionAuthenticationType.OAuth2,
        ConnectionProperties = [new("client_id")
        {
            DisplayName = "OAuth client ID",
            Description = "Google Cloud OAuth client ID for a Web application. Register Blackbird's authorization-code redirect URI."
        }, new("client_secret")
        {
            DisplayName = "OAuth client secret",
            Sensitive = true,
            Description = "Client secret from the same Google Cloud OAuth client."
        }, new("report_access")
        {
            DisplayName = "Analytics access",
            Description = "Reconnect when changing report access. Revenue reports require monetary read-only consent and an eligible channel.",
            DataItems = [new("standard", "Standard reports"), new("monetary", "Include revenue reports")]
        }

        ]
    }

    ];

    public IEnumerable<AuthenticationCredentialsProvider> CreateAuthorizationCredentialsProviders(Dictionary<string, string> values)
    {
        if (values.TryGetValue("access_token", out var token) && !string.IsNullOrWhiteSpace(token))
            return[new AuthenticationCredentialsProvider("Authorization", "Bearer " + token)];
        return values.Where(x => x.Key == "api_key").Select(x => new AuthenticationCredentialsProvider(x.Key, x.Value));
    }
}
