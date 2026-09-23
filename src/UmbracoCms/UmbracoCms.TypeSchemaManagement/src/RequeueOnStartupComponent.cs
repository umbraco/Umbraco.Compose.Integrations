using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Persistence;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal sealed class RequeueOnStartupComponent(
    ISchemaQueueRepository queueRepository,
    Channel<SchemaQueueItem> channel,
    IRuntimeState runtimeState,
    IServerRoleAccessor serverRoleAccessor,
    ILogger<RequeueOnStartupComponent> logger) : IComponent
{
    public void Initialize()
    {
        if (runtimeState.Level < RuntimeLevel.Run)
        {
            return;
        }

        if (serverRoleAccessor.CurrentServerRole is ServerRole.Subscriber)
        {
            logger.LogDebug("Skipping queue drain - Current server role is 'Subscriber'.");
            return;
        }

        IReadOnlyList<SchemaQueueDto> queueItems = Task.Run(() => queueRepository
            .GetAllAsync())
            .GetAwaiter()
            .GetResult();

        if (queueItems.Count is 0)
        {
            return;
        }

        logger.LogInformation("Draining {Count} persisted schema queue item(s) from database", queueItems.Count);

        foreach (SchemaQueueDto dto in queueItems)
        {
            Task.Run(async () => await channel.Writer
                .WriteAsync(new SchemaQueueItem(dto.Id, dto.ContentTypeAlias))
                .ConfigureAwait(false))
                .GetAwaiter()
                .GetResult();
        }

        logger.LogInformation("Finished draining persisted schema queue items");
    }

    public void Terminate()
    {
    }
}
