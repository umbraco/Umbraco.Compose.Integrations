using Umbraco.Cms.Core.Notifications;
using Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Cache.Content;

namespace Umbraco.Cms.Core.DependencyInjection;

/// <summary>
/// Extension methods for adding Umbraco Compose ingestion cache refresher notification handlers.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    /// <summary>
    /// Extension methods for adding Umbraco Compose ingestion cache refresher notification handlers.
    /// </summary>
    extension(IUmbracoBuilder builder)
    {
        /// <summary>
        /// Adds the Umbraco Compose custom cache refresher notification handlers.
        /// </summary>
        /// <returns>The Umbraco builder with the notification handlers added.</returns>
        internal IUmbracoBuilder AddComposeCustomCacheRefresherNotificationHandlers() =>
            builder
                .AddNotificationHandler<ContentPublishedNotification, PublishedContentNotificationHandler>()
                .AddNotificationHandler<ContentUnpublishedNotification, PublishedContentNotificationHandler>()
                .AddNotificationHandler<ContentMovedNotification, PublishedContentNotificationHandler>()
                .AddNotificationHandler<ContentMovedToRecycleBinNotification, PublishedContentNotificationHandler>();
    }
}
