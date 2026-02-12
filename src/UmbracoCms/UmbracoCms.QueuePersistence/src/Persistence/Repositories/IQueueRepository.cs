namespace Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence.Repositories;

/// <summary>
/// Base repository for queue persistence operations.
/// </summary>
public interface IQueueRepository
{
    /// <summary>Inserts a single queue item.</summary>
    /// <typeparam name="T">The DTO type.</typeparam>
    /// <param name="dto">The queue item to insert.</param>
    /// <param name="ct">Optional cancellation token.</param>
    Task InsertAsync<T>(T dto, CancellationToken ct = default) where T : class;

    /// <summary>Returns all queue items ordered by <see cref="IHaveCreatedAt.CreatedAt"/>.</summary>
    /// <typeparam name="T">The DTO type.</typeparam>
    /// <param name="ct">Optional cancellation token.</param>
    Task<IReadOnlyList<T>> GetAllAsync<T>(CancellationToken ct = default)
        where T : class, IHaveCreatedAt;

    /// <summary>Deletes a single queue item.</summary>
    /// <typeparam name="T">The DTO type.</typeparam>
    /// <param name="dto">The queue item to delete.</param>
    /// <param name="ct">Optional cancellation token.</param>
    Task DeleteAsync<T>(T dto, CancellationToken ct = default) where T : class;
}
