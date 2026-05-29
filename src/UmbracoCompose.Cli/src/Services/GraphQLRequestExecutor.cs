using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using UmbracoCompose.Cli.Commands;
using UmbracoCompose.Cli.Models;
using UmbracoCompose.Cli.Utilities;

namespace UmbracoCompose.Cli.Services;

internal sealed class GraphQLRequestExecutor
{
    private readonly ILogger<GraphQLRequestExecutor> _logger;
    private readonly IOAuthService _oAuthService;
    private readonly LoggingOptions _loggingOptions;
    private readonly HttpClient _httpClient;

    public GraphQLRequestExecutor(ILogger<GraphQLRequestExecutor> logger, IOAuthService oAuthService, LoggingOptions loggingOptions, HttpClient httpClient)
    {
        _logger = logger;
        _oAuthService = oAuthService;
        _loggingOptions = loggingOptions;
        _httpClient = httpClient;
    }

    public static string BuildUrl(Models.Profile profile) =>
        $"https://graphql.{profile.Region}.umbracocompose.com/{profile.ProjectAlias}/{profile.EnvironmentAlias}/";

    public async Task<GraphQLExecutionResult> ExecuteAsync(
        Models.Profile profile,
        string query,
        string contentType,
        CancellationToken cancellationToken)
    {
        // 1. Authenticate
        TokenResponse token;
        try
        {
            token = await _oAuthService.AuthenticateAsync(profile.ClientId, profile.ClientSecret, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Authentication failed");
            return GraphQLExecutionResult.CreateFailure(HttpErrorHelper.HandleHttpRequestException(ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate");
            return GraphQLExecutionResult.CreateFailure(HttpErrorHelper.HandleGenericException(ex, "Authentication"));
        }

        // 2. Build HTTP request
        StringContent requestContent = new (query, Encoding.UTF8, contentType);
        using HttpRequestMessage request = new (HttpMethod.Post, BuildUrl(profile))
        {
            Content = requestContent,
        };
        request.Headers.Authorization = new ("Bearer", token.AccessToken);
        request.Headers.Accept.Add(new ("application/graphql-response+json"));

        // 3. Send request
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GraphQL request failed");
            return GraphQLExecutionResult.CreateFailure(HttpErrorHelper.HandleHttpRequestException(ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute GraphQL request");
            return GraphQLExecutionResult.CreateFailure(HttpErrorHelper.HandleGenericException(ex, "GraphQL request"));
        }

        // 4. Handle error responses
        if (!response.IsSuccessStatusCode)
        {
            string? errorBody = null;
            try
            {
                errorBody = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reading response body");
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                if (_loggingOptions.Debug)
                {
                    _logger.LogError("Authentication failed response body: {ResponseBody}", errorBody);
                }
                else
                {
                    _logger.LogError("Authentication failed for GraphQL endpoint.");
                }
                return GraphQLExecutionResult.CreateFailure(CommandResult.Failure(
                    ExitCodes.ValidationError,
                    "Authentication failed. Check your profile credentials.",
                    errorBody));
            }

            if (_loggingOptions.Debug)
            {
                _logger.LogError("GraphQL request failed with status {StatusCode}, body: {ResponseBody}", response.StatusCode, errorBody);
            }
            else
            {
                _logger.LogError("GraphQL request failed with status {StatusCode}", response.StatusCode);
            }
            return GraphQLExecutionResult.CreateFailure(CommandResult.Failure(
                ExitCodes.RuntimeError,
                $"GraphQL request failed ({response.StatusCode}).",
                errorBody));
        }

        // 5. Return success — caller handles response parsing
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return GraphQLExecutionResult.Success(responseBody);
    }
}

internal sealed class GraphQLExecutionResult
{
    public bool IsSuccess { get; init; }
    public string? Body { get; init; }
    public CommandResult? Failure { get; init; }

    public static GraphQLExecutionResult Success(string body) =>
        new() { IsSuccess = true, Body = body };

    public static GraphQLExecutionResult CreateFailure(CommandResult failure) =>
        new() { IsSuccess = false, Failure = failure };
}
