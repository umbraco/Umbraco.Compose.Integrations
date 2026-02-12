using System.Threading.Channels;
using Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence;
using Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence.Repositories;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal sealed class IngestService : IIngestService
{
    private readonly ChannelWriter<IngestQueueItem> _writer;
    private readonly IContentQueueRepository _queueRepository;

    public IngestService(
        Channel<IngestQueueItem> channel,
        IContentQueueRepository queueRepository)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(queueRepository);
        _queueRepository = queueRepository;

        _writer = channel.Writer;
    }

    public async ValueTask EnqueueAsync(IngestQueueItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item is ContentIngestQueueItem contentIngestQueueItem)
        {
            var dto = new ContentQueueDto
            {
                Id = contentIngestQueueItem.Id,
                CreatedAt = contentIngestQueueItem.CreatedAt,
            };

            List<ContentQueuePayloadDto> payloads = contentIngestQueueItem.Entities.Select(payload => new ContentQueuePayloadDto
            {
                Id = Guid.CreateVersion7(),
                QueueItemId = contentIngestQueueItem.Id,
                ContentId = payload.Id,
                TreeChangeTypes = payload.ChangeTypes,
                AffectedCultures = string.Join(",", payload.AffectedCultures),
            }).ToList();

            await _queueRepository.InsertWithPayloadsAsync(dto, payloads, cancellationToken).ConfigureAwait(false);
        }

        await _writer.WriteAsync(item, cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask EnqueueAsync(IEnumerable<IngestQueueItem> items, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (IngestQueueItem item in items)
        {
            await EnqueueAsync(item, cancellationToken).ConfigureAwait(false);
        }
    }
}
