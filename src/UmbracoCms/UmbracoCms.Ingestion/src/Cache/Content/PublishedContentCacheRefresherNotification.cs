using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Cache.Content;

internal sealed class PublishedContentCacheRefresherNotification(
    object messageObject,
    MessageType messageType) : CacheRefresherNotification(messageObject, messageType)
{
}
