namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Persistence;

/// <summary>
/// Repository for type schema queue persistence.
/// </summary>
internal interface ISchemaQueueRepository
{
    /// <summary>Inserts a single queue item.</summary>
    /// <typeparam name="T">The DTO type.</typeparam>
    /// <param name="dto">The queue item to insert.</param>
    /// <param name="ct">Optional cancellation token.</param>
    Task InsertAsync<T>(T dto, CancellationToken ct = default) where T : class;

    /// <summary>Returns all schema queue items ordered by <see cref="SchemaQueueDto.CreatedAt"/>.</summary>
    /// <param name="ct">Optional cancellation token.</param>
    Task<IReadOnlyList<SchemaQueueDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Deletes a single queue item.</summary>
    /// <typeparam name="T">The DTO type.</typeparam>
    /// <param name="dto">The queue item to delete.</param>
    /// <param name="ct">Optional cancellation token.</param>
    Task DeleteAsync<T>(T dto, CancellationToken ct = default) where T : class;

    /// <summary>Deletes a schema queue item by its ID.</summary>
    /// <param name="id">The unique identifier of the schema queue item to delete.</param>
    /// <param name="ct">Optional cancellation token.</param>
    Task DeleteByIdAsync(Guid id, CancellationToken ct = default);
}
