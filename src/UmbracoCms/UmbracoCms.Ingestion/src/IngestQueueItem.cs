namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Base class for an item in the ingest queue.
/// </summary>
public abstract record IngestQueueItem
{
    /// <summary>
    /// Gets the unique identifier of the item.
    /// </summary>
    public Guid Id { get; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets the date and time when the item was created.
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
}
