namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Persistence;

/// <summary>
/// Repository for ingest queue persistence.
/// </summary>
internal interface IIngestQueueRepository
{
    /// <summary>Inserts a single queue item.</summary>
    /// <param name="dto">The queue item to insert.</param>
    /// <param name="ct">Optional cancellation token.</param>
    Task InsertAsync(IngestQueueDto dto, CancellationToken ct = default);

    /// <summary>Returns all ingest queue items ordered by <see cref="IngestQueueDto.CreatedAt"/>.</summary>
    /// <param name="ct">Optional cancellation token.</param>
    Task<IReadOnlyList<IngestQueueDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Deletes an ingest queue item by its ID.</summary>
    /// <param name="id">The unique identifier of the queue item to delete.</param>
    /// <param name="ct">Optional cancellation token.</param>
    Task DeleteByIdAsync(Guid id, CancellationToken ct = default);
}
