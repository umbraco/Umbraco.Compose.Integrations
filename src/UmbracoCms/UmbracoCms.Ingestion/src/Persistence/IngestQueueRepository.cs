using NPoco;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Persistence;

internal sealed class IngestQueueRepository(IScopeProvider scopeProvider) : IIngestQueueRepository
{
    public async Task InsertAsync(IngestQueueDto dto, CancellationToken ct = default)
    {
        using IScope scope = scopeProvider.CreateScope();
        await scope.Database.InsertAsync(dto, ct)
            .ConfigureAwait(false);
        scope.Complete();
    }

    public async Task<IReadOnlyList<IngestQueueDto>> GetAllAsync(CancellationToken ct = default)
    {
        using IScope scope = scopeProvider.CreateScope();
        Sql<ISqlContext> sql = scope.SqlContext.Sql()
            .Select<IngestQueueDto>()
            .From<IngestQueueDto>()
            .OrderBy<IngestQueueDto>(x => x.CreatedAt);
        return await scope.Database.FetchAsync<IngestQueueDto>(sql, ct)
            .ConfigureAwait(false);
    }

    public async Task DeleteByIdAsync(Guid id, CancellationToken ct = default)
    {
        using IScope scope = scopeProvider.CreateScope();
        Sql<ISqlContext> sql = scope.SqlContext.Sql()
            .Delete<IngestQueueDto>()
            .Where<IngestQueueDto>(x => x.Id == id);
        await scope.Database.ExecuteAsync(sql, ct).ConfigureAwait(false);
        scope.Complete();
    }
}
