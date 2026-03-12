using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Sync;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Cache.Content;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal sealed class PublishedContentCacheRefresherNotificationHandler(
    IServerRoleAccessor serverRoleAccessor,
    ICoreScopeProvider coreScopeProvider,
    ILogger<PublishedContentCacheRefresherNotificationHandler> logger,
    IIngestService ingestService,
    IOptions<UmbracoComposeOptions> composeOptions,
    IOptions<UmbracoComposeIngestionOptions> ingestionOptions
) : INotificationAsyncHandler<PublishedContentCacheRefresherNotification>
{
    public async Task HandleAsync(PublishedContentCacheRefresherNotification notification, CancellationToken cancellationToken)
    {
        if (!composeOptions.Value.IsValid)
        {
            logger.LogDebug("Skipping ingestion - Compose options are not valid.");
            return;
        }

        if (!ingestionOptions.Value.IsValid)
        {
            logger.LogDebug("Skipping ingestion - Ingestion options are not valid.");
            return;
        }

        if (serverRoleAccessor is not { CurrentServerRole: ServerRole.SchedulingPublisher or ServerRole.Single, })
        {
            logger.LogDebug(
                "Skipping ingestion - Current server role is '{ServerRole}', expected 'SchedulingPublisher' or 'Single'.",
                serverRoleAccessor.CurrentServerRole);
            return;
        }

        if (notification.MessageObject is not PublishedContentCacheRefresher.JsonPayload[] payload)
        {
            logger.LogWarning(
                "Expected payload to be of type '{ExpectedType}' but was '{ActualType}'",
                typeof(PublishedContentCacheRefresher.JsonPayload[]),
                notification.MessageObject.GetType());
            return;
        }

        List<ContentChangePayload> entities =
            [.. payload.Select(x => new ContentChangePayload(x.ContentKey, x.ChangeTypes, x.AffectedCultures)),];

        await ExecuteDeferredAsync(() => ingestService.EnqueueAsync(new ContentIngestQueueItem(entities), cancellationToken))
            .ConfigureAwait(false);
    }

    private ValueTask ExecuteDeferredAsync(Func<ValueTask> action)
    {
        DeferredActions? actions = DeferredActions.Get(coreScopeProvider);
        if (actions is not null)
        {
            actions.Add(action);
        }
        else
        {
            return action();
        }

        return default;
    }
}
