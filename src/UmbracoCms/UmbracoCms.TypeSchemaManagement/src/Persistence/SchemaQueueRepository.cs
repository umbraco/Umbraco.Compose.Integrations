using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Persistence;

internal sealed class SchemaQueueRepository(IScopeProvider scopeProvider) : ISchemaQueueRepository
{
    public async Task InsertAsync<T>(T dto, CancellationToken ct = default) where T : class
    {
        using IScope scope = scopeProvider.CreateScope();
        await scope.Database.InsertAsync(dto, ct)
            .ConfigureAwait(false);
        scope.Complete();
    }

    public async Task<IReadOnlyList<SchemaQueueDto>> GetAllAsync(CancellationToken ct = default)
    {
        using IScope scope = scopeProvider.CreateScope();
        NPoco.Sql<Cms.Infrastructure.Persistence.ISqlContext> sql = scope.SqlContext.Sql()
            .Select<SchemaQueueDto>()
            .From<SchemaQueueDto>()
            .OrderBy<SchemaQueueDto>(x => x.CreatedAt);
        return await scope.Database.FetchAsync<SchemaQueueDto>(sql, ct)
            .ConfigureAwait(false);
    }

    public async Task DeleteAsync<T>(T dto, CancellationToken ct = default) where T : class
    {
        using IScope scope = scopeProvider.CreateScope();
        await scope.Database.DeleteAsync(dto, ct)
            .ConfigureAwait(false);
        scope.Complete();
    }

    public async Task DeleteByIdAsync(Guid id, CancellationToken ct = default)
    {
        using IScope scope = scopeProvider.CreateScope();
        NPoco.Sql<Cms.Infrastructure.Persistence.ISqlContext> sql = scope.SqlContext.Sql()
            .Delete<SchemaQueueDto>()
            .Where<SchemaQueueDto>(x => x.Id == id);
        await scope.Database.ExecuteAsync(sql, ct).ConfigureAwait(false);
        scope.Complete();
    }
}
