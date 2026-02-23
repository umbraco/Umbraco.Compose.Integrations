using OpenIddict.Client;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core;

internal sealed class AuthenticationHttpMessageHandler(OpenIddictClientService client) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        OpenIddictClientModels.ClientCredentialsAuthenticationResult response = await client.AuthenticateWithClientCredentialsAsync(new()
        {
            CancellationToken = cancellationToken
        })
            .ConfigureAwait(false);

        request.Headers.Authorization = new("Bearer", response.AccessToken);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
