namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Represents an item in the content ingest queue.
/// </summary>
/// <param name="Entities">The entities to ingest.</param>
public sealed record ContentIngestQueueItem(IReadOnlyCollection<ContentChangePayload> Entities) : IngestQueueItem;
