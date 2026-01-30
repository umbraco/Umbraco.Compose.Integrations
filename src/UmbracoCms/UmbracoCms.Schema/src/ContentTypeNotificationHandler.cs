using System.Threading.Channels;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.Compose.Integrations.UmbracoCms.Schema;

internal class ContentTypeNotificationHandler(ChannelWriter<SchemaQueueItem> writer)
    : INotificationAsyncHandler<ContentTypeSavedNotification>
{
    public async Task HandleAsync(ContentTypeSavedNotification notification, CancellationToken cancellationToken)
    {
        foreach (var contentType in notification.SavedEntities)
        {
            await writer.WriteAsync(new SchemaQueueItem(contentType.Alias), cancellationToken);
        }
    }
}