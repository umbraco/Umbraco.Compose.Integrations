using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Persistence;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal sealed class IngestionQueueDrainComponent(
    IIngestQueueRepository queueRepository,
    Channel<IngestQueueItem> channel,
    IRuntimeState runtimeState,
    IServerRoleAccessor serverRoleAccessor,
    ILogger<IngestionQueueDrainComponent> logger) : IAsyncComponent
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        if (runtimeState.Level < RuntimeLevel.Run)
        {
            return;
        }

        if (serverRoleAccessor.CurrentServerRole is ServerRole.Subscriber)
        {
            logger.LogDebug("Skipping queue drain - Current server role is 'Subscriber'.");
            return;
        }

        IReadOnlyList<IngestQueueDto> queueItems = await queueRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        if (queueItems.Count is 0)
        {
            return;
        }

        logger.LogInformation("Draining {Count} persisted queue item(s) from database", queueItems.Count);

        foreach (IngestQueueDto dto in queueItems)
        {
            Type? itemType = Type.GetType(dto.ItemType);
            if (itemType?.IsAssignableTo(typeof(IngestQueueItem)) is not true)
            {
                logger.LogError(
                    "Cannot resolve type '{ItemType}' for queue item {QueueItemId}, skipping",
                    dto.ItemType,
                    dto.Id);
                continue;
            }

            IngestQueueItem? item;
            try
            {
                item = JsonSerializer.Deserialize(dto.Payload, itemType, s_jsonOptions) as IngestQueueItem;
            }
            catch (JsonException ex)
            {
                logger.LogError(
                    ex,
                    "Failed to deserialize queue item {QueueItemId} of type '{ItemType}', skipping",
                    dto.Id,
                    dto.ItemType);
                continue;
            }

            if (item is null)
            {
                logger.LogError("Deserialized queue item {QueueItemId} was null, skipping", dto.Id);
                continue;
            }

            await channel.Writer
                .WriteAsync(item, cancellationToken)
                .ConfigureAwait(false);
        }

        logger.LogInformation("Finished draining persisted queue items");
    }

    public Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
