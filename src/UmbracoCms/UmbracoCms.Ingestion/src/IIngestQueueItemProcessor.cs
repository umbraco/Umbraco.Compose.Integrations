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
    /// <returns>An enumerable of ingest entries.</returns>
    IEnumerable<IngestEntry> Process(T item);
}
