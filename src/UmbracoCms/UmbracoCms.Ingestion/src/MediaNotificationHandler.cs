using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal class MediaNotificationHandler(
    ICoreScopeProvider coreScopeProvider,
    IIngestService ingestService,
    IOptions<UmbracoComposeOptions> composeOptions,
    IOptions<UmbracoComposeIngestionOptions> ingestionOptions,
    ILogger<MediaNotificationHandler> logger
    ) :
        INotificationAsyncHandler<MediaSavedNotification>,
        INotificationAsyncHandler<MediaDeletedNotification>,
        INotificationAsyncHandler<MediaMovedNotification>,
        INotificationAsyncHandler<MediaMovedToRecycleBinNotification>
{
    public Task HandleAsync(MediaSavedNotification notification, CancellationToken cancellationToken)
    {
        if (!IsConfigurationValid())
        {
            return Task.CompletedTask;
        }

        return EnqueueAsync(notification.SavedEntities, MediaChangeType.Update, cancellationToken);
    }

    public Task HandleAsync(MediaDeletedNotification notification, CancellationToken cancellationToken)
    {
        if (!IsConfigurationValid())
        {
            return Task.CompletedTask;
        }

        return EnqueueAsync(notification.DeletedEntities, MediaChangeType.Delete, cancellationToken);
    }

    public Task HandleAsync(MediaMovedNotification notification, CancellationToken cancellationToken)
    {
        if (!IsConfigurationValid())
        {
            return Task.CompletedTask;
        }

        return EnqueueAsync(notification.MoveInfoCollection.Select(x => x.Entity), MediaChangeType.Update, cancellationToken);
    }

    public Task HandleAsync(MediaMovedToRecycleBinNotification notification, CancellationToken cancellationToken)
    {
        if (!IsConfigurationValid())
        {
            return Task.CompletedTask;
        }

        return EnqueueAsync(notification.MoveInfoCollection.Select(x => x.Entity), MediaChangeType.Delete, cancellationToken);
    }

    private async Task EnqueueAsync(IEnumerable<IMedia> media, MediaChangeType changeType, CancellationToken cancellationToken)
    {
        List<MediaChangePayload> payloads = [.. media.Select(x => new MediaChangePayload(x.Key, changeType))];

        await DeferredActions.ExecuteDeferredAsync(
            coreScopeProvider,
            () => ingestService.EnqueueAsync(new MediaIngestQueueItem(payloads), cancellationToken))
            .ConfigureAwait(false);
    }

    private bool IsConfigurationValid()
    {
        if (!composeOptions.Value.IsValid)
        {
            logger.LogDebug("Skipping ingestion - Compose options are not valid.");
            return false;
        }

        if (!string.IsNullOrEmpty(ingestionOptions.Value.GetCollectionAlias(typeof(MediaIngestQueueItem))))
        {
            return true;
        }

        logger.LogDebug("Skipping ingestion - Ingestion options are not valid.");
        return false;
    }
}
