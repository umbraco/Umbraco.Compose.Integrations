namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// An ingest queue item processor.
/// </summary>
/// <typeparam name="T">The ingest queue item type.</typeparam>
public interface IIngestQueueItemProcessor<T> where T : IngestQueueItem
{
    /// <summary>
    /// Processes the ingest queue item.
    /// </summary>
    /// <param name="item">The ingest queue item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable of ingest entries.</returns>
    IAsyncEnumerable<IngestEntry> ProcessAsync(T item, CancellationToken cancellationToken = default);
}
