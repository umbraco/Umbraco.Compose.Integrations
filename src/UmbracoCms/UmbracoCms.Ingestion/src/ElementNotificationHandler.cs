using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
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
    IOptions<UmbracoComposeOptions> composeOptions,
    IOptions<UmbracoComposeIngestionOptions> ingestionOptions,
    ILogger<ElementNotificationHandler> logger
) :
    INotificationAsyncHandler<ElementPublishedNotification>
{
    public async Task HandleAsync(ElementPublishedNotification notification, CancellationToken cancellationToken)
    {
        if (!composeOptions.Value.IsValid)
        {
            logger.LogDebug("Skipping ingestion - Compose options are not valid.");
            return;
        }

        string? contentCollectionAlias = ingestionOptions.Value.GetCollectionAlias(typeof(MediaIngestQueueItem));
        string? mediaCollectionAlias = ingestionOptions.Value.GetCollectionAlias(typeof(MediaIngestQueueItem));

        if (string.IsNullOrEmpty(contentCollectionAlias) && string.IsNullOrEmpty(mediaCollectionAlias))
        {
            logger.LogDebug("Skipping ingestion - Ingestion options are not valid.");
            return;
        }

        IEnumerable<Guid> publishedKeys = notification.PublishedEntities.Select(entity => entity.Key);
        (HashSet<Guid> documentKeys, HashSet<Guid> mediaKeys) = GetReferencingDocumentKeys(publishedKeys);

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
