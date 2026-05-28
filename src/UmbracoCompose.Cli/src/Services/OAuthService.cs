using System.Net;
using System.Text.Json;
using UmbracoCompose.Cli.Models;

namespace UmbracoCompose.Cli.Services;

internal interface IOAuthService
{
    Task<TokenResponse> AuthenticateAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default);
}

internal sealed class OAuthService : IOAuthService
{
    private const string TokenEndpoint = "https://management.umbracocompose.com/v1/auth/token";

    private readonly HttpClient _httpClient;

    public OAuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TokenResponse> AuthenticateAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
        });

        var response = await _httpClient.PostAsync(TokenEndpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
            var message = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => $"Authentication failed. Invalid credentials. {errorBody}",
                _ => $"Authentication request failed ({response.StatusCode}). {errorBody}"
            };
            throw new HttpRequestException(message, null, response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize(json, AppJsonContext.Default.TokenResponse);

        return tokenResponse ?? throw new JsonException("Empty token response.");
    }
}
