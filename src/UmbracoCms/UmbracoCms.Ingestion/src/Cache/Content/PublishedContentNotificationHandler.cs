using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Cache.Content;

// copied from Umbraco.Cms.Search
// TODO: do we need to also handle ContentDeletedNotification?
internal sealed class PublishedContentNotificationHandler(DistributedCache distributedCache) :
    ContentNotificationHandlerBase,
    IDistributedCacheNotificationHandler<ContentPublishedNotification>,
    IDistributedCacheNotificationHandler<ContentUnpublishedNotification>,
    IDistributedCacheNotificationHandler<ContentMovedNotification>,
    INotificationHandler<ContentMovedToRecycleBinNotification>
{
    private readonly DistributedCache _distributedCache = distributedCache;

    public void Handle(ContentPublishedNotification notification)
    {
        // we sometimes get unpublished entities here... filter those out, we don't need them
        IContent[] publishedEntities = [.. notification.PublishedEntities.Where(entity => entity.Published),];
        if (publishedEntities.Length == 0)
        {
            return;
        }

        // not sure if this is needed/wanted
        IContent[] topmostEntities = FindTopmostEntities(publishedEntities);
        PublishedContentCacheRefresher.JsonPayload[] payloads = [.. topmostEntities
            .Select(entity =>
            {
                // TODO: instead of merging published and unpublished cultures we need create different payloads
                // for unpublished cultures we need to send o TreeChangeTypes.Remove, otherwise RefreshNode
                // RefreshBranch is a bit uncertain, but should probably happen if the entity wasn't
                // previously published (can we detect that?), or if it's url has changed (again can we detect that?)
                IEnumerable<string> publishedCultures = entity.CultureInfos?.Values
                    .Where(x => entity.WasPropertyDirty(ContentBase.ChangeTrackingPrefix.PublishedCulture + x.Culture))
                    .Select(x => x.Culture)
                    ?? [];
                IEnumerable<string> unpublishedCultures = entity.CultureInfos?.Values
                    .Where(x => entity.WasPropertyDirty(ContentBase.ChangeTrackingPrefix.UnpublishedCulture + x.Culture))
                    .Select(x => x.Culture)
                    ?? [];
                bool wasUnpublished = entity.WasPropertyDirty(nameof(IContent.Published));

                string[] affectedCultures = [.. publishedCultures.Union(unpublishedCultures).Distinct(),];
                return new PublishedContentCacheRefresher.JsonPayload(
                    entity.Key,
                    wasUnpublished || affectedCultures.Length > 0 ? TreeChangeTypes.RefreshBranch : TreeChangeTypes.RefreshNode,
                    affectedCultures);
            })
            .WhereNotNull(),];

        _distributedCache.RefreshByPayload(PublishedContentCacheRefresher.UniqueId, payloads);
    }

    public void Handle(ContentUnpublishedNotification notification)
    {
        // TODO: would be nice if we could include the unpublished cultures in the payload, maybe it's fine if we just send
        // delete entry for all cultures, if doctype varies by culture, or null if it doesn't.
        PublishedContentCacheRefresher.JsonPayload[] payloads = [.. notification
            .UnpublishedEntities
            .Select(static entity => new PublishedContentCacheRefresher.JsonPayload(entity.Key, TreeChangeTypes.Remove, [])),];

        _distributedCache.RefreshByPayload(PublishedContentCacheRefresher.UniqueId, payloads);
    }

    public void Handle(ContentMovedNotification notification)
    {
        HandleMove(notification.MoveInfoCollection, TreeChangeTypes.RefreshBranch);
    }

    public void Handle(ContentMovedToRecycleBinNotification notification)
    {
        HandleMove(notification.MoveInfoCollection, TreeChangeTypes.Remove);
    }

    private void HandleMove(IEnumerable<MoveEventInfoBase<IContent>> moveEventInfo, TreeChangeTypes changeType)
    {
        IContent[] topmostEntities = FindTopmostEntities(moveEventInfo.Select(i => i.Entity));
        PublishedContentCacheRefresher.JsonPayload[] payloads = [.. topmostEntities.Select(
            entity => new PublishedContentCacheRefresher.JsonPayload(entity.Key, changeType, [])),];

        _distributedCache.RefreshByPayload(PublishedContentCacheRefresher.UniqueId, payloads);
    }
}
