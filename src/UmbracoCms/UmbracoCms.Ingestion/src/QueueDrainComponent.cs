using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;
using Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence;
using Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence.Repositories;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal sealed class QueueDrainComponent(
    IContentQueueRepository queueRepository,
    Channel<IngestQueueItem> channel,
    IRuntimeState runtimeState,
    ILogger<QueueDrainComponent> logger) : IAsyncComponent
{
    public async Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        if (runtimeState.Level < RuntimeLevel.Run)
        {
            return;
        }

        IReadOnlyList<ContentQueueDto> queueItems = await queueRepository
            .GetAllAsync<ContentQueueDto>(cancellationToken)
            .ConfigureAwait(false);

        if (queueItems.Count is 0)
        {
            return;
        }

        logger.LogInformation("Draining {Count} persisted queue item(s) from database", queueItems.Count);

        IReadOnlyList<ContentQueuePayloadDto> allPayloads = await queueRepository
            .GetAllPayloadsAsync(cancellationToken)
            .ConfigureAwait(false);

        ILookup<Guid, ContentQueuePayloadDto> grouped = allPayloads.ToLookup(p => p.QueueItemId);

        foreach (ContentQueueDto queueItem in queueItems)
        {
            List<ContentChangePayload> entities = grouped[queueItem.Id]
                .Select(static p => new ContentChangePayload(
                    p.ContentId,
                    p.TreeChangeTypes,
                    p.AffectedCultures?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? []))
                .ToList();

            if (entities.Count is 0)
            {
                logger.LogWarning("Queue item {QueueItemId} has no payloads, skipping", queueItem.Id);
                await queueRepository.DeleteByQueueItemIdAsync(queueItem.Id, cancellationToken).ConfigureAwait(false);
                continue;
            }

            await channel.Writer
                .WriteAsync(new ContentIngestQueueItem(entities), cancellationToken)
                .ConfigureAwait(false);

            await queueRepository
                .DeleteByQueueItemIdAsync(queueItem.Id, cancellationToken)
                .ConfigureAwait(false);
        }

        logger.LogInformation("Finished draining persisted queue items");
    }

    public Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken) => Task.CompletedTask;
}
