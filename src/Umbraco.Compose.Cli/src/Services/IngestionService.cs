using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Serialization;
using Umbraco.Compose.Cli.Commands;
using Umbraco.Compose.Cli.Models;
using Umbraco.Compose.Cli.Utilities;
using Umbraco.Compose.Cli.Clients;
using Umbraco.Compose.Cli.Clients.Ingestion;
using Umbraco.Compose.Cli.Clients.Ingestion.Models;
using Microsoft.Kiota.Serialization.Json;

namespace Umbraco.Compose.Cli.Services;

internal sealed class IngestionService
{
    private readonly IOAuthService _oAuthService;
    private readonly IngestionApiClientFactory _ingestionClientFactory;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(IOAuthService oAuthService, IngestionApiClientFactory ingestionClientFactory, ILogger<IngestionService> logger)
    {
        _oAuthService = oAuthService;
        _ingestionClientFactory = ingestionClientFactory;
        _logger = logger;
    }

    public async Task<CommandResult> IngestAsync(Models.Profile profile, string collectionAlias, string? functionAlias, string dataJson, CancellationToken cancellationToken = default)
    {
        TokenResponse token;
        try
        {
            token = await _oAuthService.AuthenticateAsync(profile.ClientId, profile.ClientSecret, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Authentication failed for ingestion");
            return HttpErrorHelper.HandleHttpRequestException(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate for ingestion");
            return HttpErrorHelper.HandleGenericException(ex, "Authentication");
        }

        IngestionApiClient client = _ingestionClientFactory.GetClient(profile.Region, token.AccessToken);

        try
        {
            if (string.IsNullOrEmpty(functionAlias))
            {
                return await BatchIngestAsync(client, profile, collectionAlias, dataJson, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await FunctionIngestAsync(client, profile, collectionAlias, functionAlias, dataJson, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (ProblemDetails ex)
        {
            _logger.LogError(ex, "Ingestion API error for collection '{CollectionAlias}'", collectionAlias);
            return CommandResult.Failure(ExitCodes.ValidationError, ex.Title, JsonSerializer.Serialize(ex.Errors, AppJsonContext.Default.ListProblemDetails_Error));
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON data for collection '{CollectionAlias}'", collectionAlias);
            return CommandResult.Failure(ExitCodes.ValidationError, $"Invalid JSON: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed during ingestion for collection '{CollectionAlias}'", collectionAlias);
            return HttpErrorHelper.HandleHttpRequestException(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed during ingestion for collection '{CollectionAlias}'", collectionAlias);
            return HttpErrorHelper.HandleGenericException(ex, "Ingestion");
        }
    }

    private async Task<CommandResult> BatchIngestAsync(IngestionApiClient client, Models.Profile profile, string collectionAlias, string dataJson, CancellationToken cancellationToken)
    {
        var data = JsonSerializer.Deserialize<List<ContentEntry>>(dataJson, AppJsonContext.Default.ListContentEntry)!;

        await client.V1[profile.ProjectAlias][profile.EnvironmentAlias][collectionAlias].PutAsync(data, cancellationToken: cancellationToken);

        return CommandResult.Success();
    }

    private async Task<CommandResult> FunctionIngestAsync(IngestionApiClient client, Models.Profile profile, string collectionAlias, string functionAlias, string dataJson, CancellationToken cancellationToken)
    {
        var element = JsonSerializer.Deserialize<JsonElement>(dataJson, AppJsonContext.Default.JsonElement);

        JsonParseNode node = new (element);

        // suppressing null is acceptable when using UntypedNode as the factory is not used
        UntypedNode untypedNode = node.GetObjectValue<UntypedNode>(null!);

        await client.V1[profile.ProjectAlias][profile.EnvironmentAlias][collectionAlias][functionAlias]
            .PutAsync(untypedNode, cancellationToken: cancellationToken);

        return CommandResult.Success();
    }
}
