using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence.Repositories;

internal abstract class QueueRepository(IScopeProvider scopeProvider) : IQueueRepository
{
    protected IScopeProvider ScopeProvider { get; } = scopeProvider;

    public async Task InsertAsync<T>(T dto, CancellationToken ct = default) where T : class
    {
        using var scope = ScopeProvider.CreateScope();
        await scope.Database.InsertAsync(dto, ct)
            .ConfigureAwait(false);
        scope.Complete();
    }

    public async Task<IReadOnlyList<T>> GetAllAsync<T>(CancellationToken ct = default)
        where T : class, IHaveCreatedAt
    {
        using var scope = ScopeProvider.CreateScope();
        var sql = scope.SqlContext.Sql()
            .Select<T>()
            .From<T>()
            .OrderBy<T>(x => x.CreatedAt);
        return await scope.Database.FetchAsync<T>(sql, ct)
            .ConfigureAwait(false);
    }

    public async Task DeleteAsync<T>(T dto, CancellationToken ct = default) where T : class
    {
        using var scope = ScopeProvider.CreateScope();
        await scope.Database.DeleteAsync(dto, ct)
            .ConfigureAwait(false);
        scope.Complete();
    }
}
