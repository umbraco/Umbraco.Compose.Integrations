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

        if (!ingestionOptions.Value.IsValid)
        {
            logger.LogDebug("Skipping ingestion - Ingestion options are not valid.");
            return;
        }

        IEnumerable<Guid> publishedKeys = notification.PublishedEntities.Select(entity => entity.Key);
        HashSet<Guid> documentKeys = GetReferencingDocumentKeys(publishedKeys);

        if (documentKeys.Count == 0)
        {
            return;
        }

        ContentChangePayload[] payloads =
        [
            .. documentKeys.Select(key => new ContentChangePayload(key, ContentChangeType.Update, []))
        ];
        await EnqueueAsync(payloads, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Walks the umbElement relations upwards from the given elements, collecting the keys of every
    /// document referencing them.
    /// </summary>
    /// <param name="elementKeys">The keys of the elements to walk upwards from.</param>
    /// <returns>The keys of the referencing documents.</returns>
    private HashSet<Guid> GetReferencingDocumentKeys(IEnumerable<Guid> elementKeys)
    {
        HashSet<Guid> documentKeys = [];
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

                if (parentType is not (UmbracoObjectTypes.Document or UmbracoObjectTypes.Element))
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

                if (parentType is UmbracoObjectTypes.Document)
                {
                    documentKeys.Add(parentKey.Result);
                }
                else
                {
                    pending.Enqueue(parentKey.Result);
                }
            }
        }

        return documentKeys;
    }

    private async Task EnqueueAsync(IReadOnlyCollection<ContentChangePayload> payloads, CancellationToken cancellationToken)
    {
        await DeferredActions.ExecuteDeferredAsync(
            coreScopeProvider,
            () => ingestService.EnqueueAsync(new ContentIngestQueueItem(payloads), cancellationToken))
            .ConfigureAwait(false);
    }
}
