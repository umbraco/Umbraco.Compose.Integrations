namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

public interface IngestQueueItemProcessor<T> where T : IngestQueueItem
{
    IAsyncEnumerable<IngestEntry> ProcessAsync(T item, CancellationToken cancellationToken = default);
}
