using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal class ContentNotificationHandler(
    ICoreScopeProvider coreScopeProvider,
    IIngestService ingestService,
    IOptions<UmbracoComposeOptions> composeOptions,
    IOptions<UmbracoComposeIngestionOptions> ingestionOptions,
    ILogger<ContentNotificationHandler> logger
    ) :
        INotificationAsyncHandler<ContentPublishedNotification>,
        INotificationAsyncHandler<ContentUnpublishedNotification>,
        INotificationAsyncHandler<ContentDeletedNotification>,
        INotificationAsyncHandler<ContentMovedNotification>,
        INotificationAsyncHandler<ContentMovedToRecycleBinNotification>
{
    public async Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken)
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

        // We sometimes get unpublished entities here, discard them.
        IContent[] publishedEntities = [.. notification.PublishedEntities.Where(entity => entity.Published)];

        if (publishedEntities.Length == 0)
        {
            return;
        }

        ContentChangePayload[] payloads = [..
            publishedEntities.Select(entity =>
            {
                string[] publishedCultures = [.. entity.AvailableCultures
                    .Where(culture => notification.HasPublishedCulture(entity, culture))];

                // Unpublishing any culture that isn't the last published one will trigger a
                // ContentPublishedNotification; we need to handle this case.
                string[] unpublishedCultures = [.. entity.AvailableCultures
                    .Where(culture => notification.HasUnpublishedCulture(entity, culture))];

                // We currently assume that a notification generates only one type of change per
                // content item - i.e. a given content item will only have publishedCultures or
                // unpublishedCultures. It does look like there are probably ways to generate
                // changes that contain both. However, such cases would seem to require manually
                // intervening and setting the publish state directly on content items rather than
                // using available methods on an IContentService.
                //
                // If this proves to happen more frequently than anticipated, we'll want to change
                // the internal channel message to use a dictionary of culture => change type
                // instead of a single change type.
                string[] affectedCultures = [.. publishedCultures.Union(unpublishedCultures) ];

                // Invariant content that was not previously published should also cause
                // descendants to be updated.
                bool contentWasPreviouslyUnpublished = entity.WasPropertyDirty(nameof(IContent.Published));

                // Publishes and unpublishes of variants should also update descendants. The only
                // thing that shouldn't is re-publishes.
                //
                // TODO: There are some cases in which we want to send UpdateWithDescendants where
                // we presently only send an Update. For example, if changed properties affected
                // the route.
                ContentChangeType changeType = contentWasPreviouslyUnpublished || affectedCultures.Length > 0 ?
                    ContentChangeType.UpdateWithDescendants :
                    ContentChangeType.Update;

                return new ContentChangePayload(
                    entity.Key,
                    changeType,
                    affectedCultures);
            })
        ];

        await EnqueueAsync(payloads, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(ContentUnpublishedNotification notification, CancellationToken cancellationToken)
    {
        // TODO: We currently implicitly send a payload representing all cultures by using an empty
        // set. If there exists in Compose some variant of an item not sourced from the CMS, then
        // it will be deleted. This isn't really a supported scenario, but it would be nice to send
        // only the cultures that were unpublished. However, there is no elegant way to retrieve
        // those from only this notification.
        ContentChangePayload[] payloads = [..
            notification.UnpublishedEntities.Select(static entity =>
                new ContentChangePayload(entity.Key, ContentChangeType.Delete, []))
            ];

        await EnqueueAsync(payloads, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(ContentDeletedNotification notification, CancellationToken cancellationToken)
    {
        ContentChangePayload[] payloads = [..
            notification.DeletedEntities.Select(static entity =>
                new ContentChangePayload(entity.Key, ContentChangeType.Delete, []))
            ];

        await EnqueueAsync(payloads, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(ContentMovedNotification notification, CancellationToken cancellationToken)
    {
        await HandleMoveAsync(notification.MoveInfoCollection, ContentChangeType.UpdateWithDescendants, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task HandleAsync(ContentMovedToRecycleBinNotification notification, CancellationToken cancellationToken)
    {
        await HandleMoveAsync(notification.MoveInfoCollection, ContentChangeType.Delete, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task HandleMoveAsync(
        IEnumerable<MoveEventInfoBase<IContent>> moveEventInfo,
        ContentChangeType changeType,
        CancellationToken cancellationToken)
    {
        ContentChangePayload[] payloads = [.. moveEventInfo.Select(move =>
            new ContentChangePayload(move.Entity.Key, changeType, []))];

        await EnqueueAsync(payloads, cancellationToken).ConfigureAwait(false);
    }

    public async Task EnqueueAsync(ContentChangePayload[] payloads, CancellationToken cancellationToken)
    {
        await DeferredActions.ExecuteDeferredAsync(
            coreScopeProvider,
            () => ingestService.EnqueueAsync(new ContentIngestQueueItem(payloads), cancellationToken))
            .ConfigureAwait(false);
    }
}
