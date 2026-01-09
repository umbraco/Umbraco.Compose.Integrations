using Umbraco.Cms.Core.Notifications;
using Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Cache.Content;

namespace Umbraco.Cms.Core.DependencyInjection;

public static partial class UmbracoBuilderExtensions
{
    extension(IUmbracoBuilder builder)
    {
        public IUmbracoBuilder AddComposeCustomCacheRefresherNotificationHandlers() =>
            builder
                .AddNotificationHandler<ContentPublishedNotification, PublishedContentNotificationHandler>()
                .AddNotificationHandler<ContentUnpublishedNotification, PublishedContentNotificationHandler>()
                .AddNotificationHandler<ContentMovedNotification, PublishedContentNotificationHandler>()
                .AddNotificationHandler<ContentMovedToRecycleBinNotification, PublishedContentNotificationHandler>();
    }
}
