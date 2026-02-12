namespace Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence.Repositories;

/// <summary>
/// Repository for content ingestion queue persistence.
/// </summary>
public interface IContentQueueRepository : IQueueRepository
{
    /// <summary>Inserts a content queue item with its associated payloads atomically.</summary>
    /// <param name="queueItem">The content queue item to insert.</param>
    /// <param name="payloads">The payload entries associated with the queue item.</param>
    /// <param name="ct">Optional cancellation token.</param>
    Task InsertWithPayloadsAsync(ContentQueueDto queueItem, IEnumerable<ContentQueuePayloadDto> payloads, CancellationToken ct = default);

    /// <summary>Deletes a content queue item and all its payloads by queue item ID.</summary>
    /// <param name="queueItemId">The unique identifier of the queue item to delete.</param>
    /// <param name="ct">Optional cancellation token.</param>
    Task DeleteByQueueItemIdAsync(Guid queueItemId, CancellationToken ct = default);

    /// <summary>Returns all content queue payloads ordered by <see cref="ContentQueuePayloadDto.QueueItemId"/>.</summary>
    /// <param name="ct">Optional cancellation token.</param>
    Task<IReadOnlyList<ContentQueuePayloadDto>> GetAllPayloadsAsync(CancellationToken ct = default);
}
