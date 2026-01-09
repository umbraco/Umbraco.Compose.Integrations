namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

public abstract record IngestQueueItem
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
}
