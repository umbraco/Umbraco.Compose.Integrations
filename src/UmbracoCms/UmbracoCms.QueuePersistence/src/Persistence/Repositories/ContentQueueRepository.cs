using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence.Repositories;

internal sealed class ContentQueueRepository(IScopeProvider scopeProvider)
    : QueueRepository(scopeProvider), IContentQueueRepository
{
    public async Task InsertWithPayloadsAsync(
        ContentQueueDto queueItem,
        IEnumerable<ContentQueuePayloadDto> payloads,
        CancellationToken ct = default)
    {
        using var scope = ScopeProvider.CreateScope();
        await scope.Database.InsertAsync(queueItem, ct).ConfigureAwait(false);
        await scope.Database.InsertBulkAsync(payloads, cancellationToken: ct).ConfigureAwait(false);
        scope.Complete();
    }

    public async Task DeleteByQueueItemIdAsync(Guid queueItemId, CancellationToken ct = default)
    {
        using var scope = ScopeProvider.CreateScope();

        // Delete payloads first (child rows)
        var deletePayloads = scope.SqlContext.Sql()
            .Delete<ContentQueuePayloadDto>()
            .Where<ContentQueuePayloadDto>(x => x.QueueItemId == queueItemId);
        await scope.Database.ExecuteAsync(deletePayloads, ct).ConfigureAwait(false);

        // Delete the queue item (parent row)
        var deleteQueueItem = scope.SqlContext.Sql()
            .Delete<ContentQueueDto>()
            .Where<ContentQueueDto>(x => x.Id == queueItemId);
        await scope.Database.ExecuteAsync(deleteQueueItem, ct).ConfigureAwait(false);

        scope.Complete();
    }

    public async Task<IReadOnlyList<ContentQueuePayloadDto>> GetAllPayloadsAsync(CancellationToken ct = default)
    {
        using var scope = ScopeProvider.CreateScope();
        var sql = scope.SqlContext.Sql()
            .Select<ContentQueuePayloadDto>()
            .From<ContentQueuePayloadDto>()
            .OrderBy<ContentQueuePayloadDto>(x => x.QueueItemId);
        return await scope.Database.FetchAsync<ContentQueuePayloadDto>(sql, ct)
            .ConfigureAwait(false);
    }
}
