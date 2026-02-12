using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence.Repositories;

internal sealed class SchemaQueueRepository(IScopeProvider scopeProvider)
    : QueueRepository(scopeProvider), ISchemaQueueRepository
{
    public async Task DeleteByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var scope = ScopeProvider.CreateScope();
        var sql = scope.SqlContext.Sql()
            .Delete<SchemaQueueDto>()
            .Where<SchemaQueueDto>(x => x.Id == id);
        await scope.Database.ExecuteAsync(sql, ct).ConfigureAwait(false);
        scope.Complete();
    }
}
