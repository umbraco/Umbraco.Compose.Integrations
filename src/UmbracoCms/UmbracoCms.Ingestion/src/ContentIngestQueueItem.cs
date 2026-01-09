namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

public sealed record ContentIngestQueueItem(IReadOnlyCollection<ContentChangePayload> Entities) : IngestQueueItem;
