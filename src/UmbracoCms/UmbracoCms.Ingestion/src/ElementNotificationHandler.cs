using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Compose.Integrations.UmbracoCms.Core;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal class ElementNotificationHandler(
    ICoreScopeProvider coreScopeProvider,
    IIdKeyMap idKeyMap,
    IIngestService ingestService,
    IRelationService relationService,
    IRequestCache requestCache,
    IOptions<UmbracoComposeOptions> composeOptions,
    IOptions<UmbracoComposeIngestionOptions> ingestionOptions,
    ILogger<ElementNotificationHandler> logger
) :
    INotificationAsyncHandler<ElementPublishedNotification>,
    INotificationAsyncHandler<ElementUnpublishedNotification>,
    INotificationAsyncHandler<ElementDeletingNotification>,
    INotificationAsyncHandler<ElementDeletedNotification>,
    INotificationAsyncHandler<ElementMovedToRecycleBinNotification>
{
    private static readonly string PendingDeletesCacheKey =
        $"{typeof(ElementNotificationHandler).FullName}.PendingDeletes";

    public async Task HandleAsync(ElementPublishedNotification notification, CancellationToken cancellationToken)
    {
        if (!IsConfigurationValid())
        {
            return;
        }

        (HashSet<Guid> documentKeys, HashSet<Guid> mediaKeys) = GetReferencingDocumentKeys(
            notification.PublishedEntities.Select(x => x.Key));

        await EnqueueAsync(documentKeys, mediaKeys, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(ElementUnpublishedNotification notification, CancellationToken cancellationToken)
    {
        if (!IsConfigurationValid())
        {
            return;
        }

        (HashSet<Guid> documentKeys, HashSet<Guid> mediaKeys) = GetReferencingDocumentKeys(
            notification.UnpublishedEntities.Select(x => x.Key));

        await EnqueueAsync(documentKeys, mediaKeys, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(ElementDeletingNotification notification, CancellationToken cancellationToken)
    {
        if (!IsConfigurationValid())
        {
            return;
        }

        if (!requestCache.IsAvailable)
        {
            (HashSet<Guid> documentKeys, HashSet<Guid> mediaKeys) = GetReferencingDocumentKeys(
                notification.DeletedEntities.Select(x => x.Key));

            await EnqueueAsync(documentKeys, mediaKeys, cancellationToken).ConfigureAwait(false);
            return;
        }

        var pendingDeletes =
            (Dictionary<Guid, (HashSet<Guid> DocumentKeys, HashSet<Guid> MediaKeys)>)requestCache.Get(
                PendingDeletesCacheKey,
                () => new Dictionary<Guid, (HashSet<Guid> DocumentKeys, HashSet<Guid> MediaKeys)>())!;

        foreach (Guid elementKey in notification.DeletedEntities.Select(x => x.Key))
        {
            pendingDeletes[elementKey] = GetReferencingDocumentKeys([elementKey]);
        }
    }

    public async Task HandleAsync(ElementDeletedNotification notification, CancellationToken cancellationToken)
    {
        if (!IsConfigurationValid()
            || requestCache.Get(PendingDeletesCacheKey)
                is not Dictionary<Guid, (HashSet<Guid> DocumentKeys, HashSet<Guid> MediaKeys)> pendingDeletes)
        {
            return;
        }

        HashSet<Guid> documentKeys = [];
        HashSet<Guid> mediaKeys = [];

        foreach (Guid elementKey in notification.DeletedEntities.Select(x => x.Key))
        {
            if (pendingDeletes.Remove(elementKey, out (HashSet<Guid> DocumentKeys, HashSet<Guid> MediaKeys) references))
            {
                documentKeys.UnionWith(references.DocumentKeys);
                mediaKeys.UnionWith(references.MediaKeys);
            }
        }

        await EnqueueAsync(documentKeys, mediaKeys, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(ElementMovedToRecycleBinNotification notification, CancellationToken cancellationToken)
    {
        if (!IsConfigurationValid())
        {
            return;
        }

        (HashSet<Guid> documentKeys, HashSet<Guid> mediaKeys) = GetReferencingDocumentKeys(
            notification.MoveInfoCollection.Select(x => x.Entity).Where(x => x.Published).Select(x => x.Key));

        await EnqueueAsync(documentKeys, mediaKeys, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnqueueAsync(HashSet<Guid> documentKeys, HashSet<Guid> mediaKeys, CancellationToken cancellationToken)
    {
        if (documentKeys.Count > 0)
        {
            ContentChangePayload[] payloads =
            [
                .. documentKeys.Select(key => new ContentChangePayload(key, ContentChangeType.Update, []))
            ];

            await DeferredActions.ExecuteDeferredAsync(
                coreScopeProvider,
                () => ingestService.EnqueueAsync(new ContentIngestQueueItem(payloads), cancellationToken))
                .ConfigureAwait(false);
        }

        if (mediaKeys.Count > 0)
        {
            MediaChangePayload[] payloads =
            [
                .. mediaKeys.Select(key => new MediaChangePayload(key, MediaChangeType.Update))
            ];

            await DeferredActions.ExecuteDeferredAsync(
                coreScopeProvider,
                () => ingestService.EnqueueAsync(new MediaIngestQueueItem(payloads), cancellationToken))
                .ConfigureAwait(false);
        }
    }

    private bool IsConfigurationValid()
    {
        if (!composeOptions.Value.IsValid)
        {
            logger.LogDebug("Skipping ingestion - Compose options are not valid.");
            return false;
        }

        string? contentCollectionAlias = ingestionOptions.Value.GetCollectionAlias(typeof(ContentIngestQueueItem));
        string? mediaCollectionAlias = ingestionOptions.Value.GetCollectionAlias(typeof(MediaIngestQueueItem));

        if (!string.IsNullOrEmpty(contentCollectionAlias) || !string.IsNullOrEmpty(mediaCollectionAlias))
        {
            return true;
        }

        logger.LogDebug("Skipping ingestion - Ingestion options are not valid.");
        return false;
    }

    /// <summary>
    /// Walks the umbElement relations upwards from the given elements, collecting the keys of every
    /// document referencing them.
    /// </summary>
    /// <param name="elementKeys">The keys of the elements to walk upwards from.</param>
    /// <returns>The keys of the referencing documents.</returns>
    private (HashSet<Guid> DocumentKeys, HashSet<Guid> MediaKeys) GetReferencingDocumentKeys(IEnumerable<Guid> elementKeys)
    {
        HashSet<Guid> documentKeys = [];
        HashSet<Guid> mediaKeys = [];
        HashSet<Guid> visited = [];
        Queue<Guid> pending = new(elementKeys);

        while (pending.TryDequeue(out Guid elementKey))
        {
            if (!visited.Add(elementKey))
            {
                continue;
            }

            Attempt<int> elementId = idKeyMap.GetIdForKey(elementKey, UmbracoObjectTypes.Element);

            if (!elementId.Success)
            {
                logger.LogWarning("Could not resolve an id for element {ElementKey}", elementKey);
                continue;
            }

            IEnumerable<IRelation> relations = relationService.GetByChildId(
                elementId.Result,
                Constants.Conventions.RelationTypes.RelatedElementAlias);

            foreach (IRelation relation in relations)
            {
                UmbracoObjectTypes parentType = ObjectTypes.GetUmbracoObjectType(relation.ParentObjectType);

                if (parentType is not (UmbracoObjectTypes.Document or UmbracoObjectTypes.Element or UmbracoObjectTypes.Media))
                {
                    logger.LogDebug(
                        "Ignoring reference to element {ElementKey} from unsupported entity type {ParentType}",
                        elementKey,
                        parentType);
                    continue;
                }

                Attempt<Guid> parentKey = idKeyMap.GetKeyForId(relation.ParentId, parentType);

                if (!parentKey.Success)
                {
                    logger.LogWarning(
                        "Could not resolve a key for {ParentType} with id {ParentId} referencing element {ElementKey}",
                        parentType,
                        relation.ParentId,
                        elementKey);
                    continue;
                }

                switch (parentType)
                {
                    case UmbracoObjectTypes.Document:
                        documentKeys.Add(parentKey.Result);
                        break;
                    case UmbracoObjectTypes.Media:
                        mediaKeys.Add(parentKey.Result);
                        break;
                    default:
                        pending.Enqueue(parentKey.Result);
                        break;
                }
            }
        }

        return (documentKeys, mediaKeys);
    }
}
