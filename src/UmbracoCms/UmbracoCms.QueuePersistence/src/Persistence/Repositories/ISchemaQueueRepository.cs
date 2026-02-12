namespace Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence.Repositories;

/// <summary>
/// Repository for type schema queue persistence.
/// </summary>
public interface ISchemaQueueRepository : IQueueRepository
{
    /// <summary>Deletes a schema queue item by its ID.</summary>
    /// <param name="id">The unique identifier of the schema queue item to delete.</param>
    /// <param name="ct">Optional cancellation token.</param>
    Task DeleteByIdAsync(Guid id, CancellationToken ct = default);
}
