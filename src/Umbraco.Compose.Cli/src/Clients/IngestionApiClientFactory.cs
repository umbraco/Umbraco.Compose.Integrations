using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Umbraco.Compose.Cli.Clients.Ingestion;

namespace Umbraco.Compose.Cli.Clients;

internal sealed class IngestionApiClientFactory(HttpClient httpClient)
{
    public IngestionApiClient GetClient(string region, string bearerToken)
    {
        BaseBearerTokenAuthenticationProvider authProvider = new(new BearerTokenAccessTokenProvider(bearerToken));

        HttpClientRequestAdapter adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient)
        {
            BaseUrl = $"https://ingest.{region}.umbracocompose.com",
        };

        return new(adapter);
    }

    private sealed class BearerTokenAccessTokenProvider(string bearerToken) : IAccessTokenProvider
    {
        public AllowedHostsValidator AllowedHostsValidator => AllowedHostsValidator;

        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(bearerToken);
    }
}
