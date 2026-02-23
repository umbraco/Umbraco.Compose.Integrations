using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Persistence;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal sealed class SchemaQueueDrainComponent(
    ISchemaQueueRepository queueRepository,
    Channel<SchemaQueueItem> channel,
    IRuntimeState runtimeState,
    IServerRoleAccessor serverRoleAccessor,
    ILogger<SchemaQueueDrainComponent> logger) : IAsyncComponent
{
    public async Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        if (runtimeState.Level < RuntimeLevel.Run)
        {
            return;
        }

        if (serverRoleAccessor is { CurrentServerRole: ServerRole.Subscriber or ServerRole.SchedulingPublisher })
        {
            return;
        }

        IReadOnlyList<SchemaQueueDto> queueItems = await queueRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        if (queueItems.Count is 0)
        {
            return;
        }

        logger.LogInformation("Draining {Count} persisted schema queue item(s) from database", queueItems.Count);

        foreach (SchemaQueueDto dto in queueItems)
        {
            await channel.Writer
                .WriteAsync(new SchemaQueueItem(dto.Id, dto.ContentTypeAlias), cancellationToken)
                .ConfigureAwait(false);
        }

        logger.LogInformation("Finished draining persisted schema queue items");
    }

    public Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken) => Task.CompletedTask;
}
