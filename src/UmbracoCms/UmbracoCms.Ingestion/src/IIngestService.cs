namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

public interface IIngestService
{
    ValueTask EnqueueAsync(IngestQueueItem item, CancellationToken cancellationToken = default);
    ValueTask EnqueueAsync(IEnumerable<IngestQueueItem> items, CancellationToken cancellationToken = default);
}
