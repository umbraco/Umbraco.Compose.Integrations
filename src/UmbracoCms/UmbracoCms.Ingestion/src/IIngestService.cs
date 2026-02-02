namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Service for enqueueing data to be ingested.
/// </summary>
public interface IIngestService
{
    /// <summary>
    /// Enqueues an item to be ingested.
    /// </summary>
    /// <param name="item">The item to be ingested.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask EnqueueAsync(IngestQueueItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueues a batch of items to be ingested.
    /// </summary>
    /// <param name="items">The items to be ingested.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask EnqueueAsync(IEnumerable<IngestQueueItem> items, CancellationToken cancellationToken = default);
}
