using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Persistence;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal sealed class ContentTypeNotificationHandler(
    ChannelWriter<SchemaQueueItem> writer,
    ISchemaQueueRepository queueRepository,
    ILogger<ContentTypeNotificationHandler> logger,
    IOptions<UmbracoComposeOptions> options)
    : INotificationAsyncHandler<ContentTypeSavedNotification>
{
    public async Task HandleAsync(ContentTypeSavedNotification notification, CancellationToken cancellationToken)
    {
        if (!options.Value.IsValid)
        {
            logger.LogDebug("Skipping type schema update - Compose options are not valid.");
            return;
        }


        foreach (IContentType contentType in notification.SavedEntities)
        {
            var dto = new SchemaQueueDto
            {
                Id = Guid.CreateVersion7(),
                CreatedAt = DateTime.UtcNow,
                ContentTypeAlias = contentType.Alias,
            };

            await queueRepository.InsertAsync(dto, cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync(new SchemaQueueItem(dto.Id, contentType.Alias), cancellationToken).ConfigureAwait(false);
        }
    }
}
