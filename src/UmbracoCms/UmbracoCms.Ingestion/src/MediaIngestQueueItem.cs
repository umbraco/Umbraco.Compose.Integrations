namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Represents an item in the media ingest queue.
/// </summary>
/// <param name="Entities">The entities to ingest.</param>
public sealed record MediaIngestQueueItem(IReadOnlyCollection<MediaChangePayload> Entities) : IngestQueueItem;
