using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Persistence;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal sealed class ContentTypeNotificationHandler(
    ChannelWriter<SchemaQueueItem> writer,
    ISchemaQueueRepository queueRepository,
    IDataTypeService dataTypeService,
    ILogger<ContentTypeNotificationHandler> logger,
    IOptions<UmbracoComposeOptions> options) :
        INotificationAsyncHandler<ContentTypeSavedNotification>,
        INotificationAsyncHandler<DataTypeSavedNotification>
{
    public async Task HandleAsync(ContentTypeSavedNotification notification, CancellationToken cancellationToken)
    {
        if (!options.Value.IsValid)
        {
            logger.LogDebug("Skipping type schema update - Compose options are not valid.");
            return;
        }

        foreach (string contentTypeAlias in notification.SavedEntities.Select(contentType => contentType.Alias))
        {
            SchemaQueueDto dto = new()
            {
                Id = Guid.CreateVersion7(),
                CreatedAt = DateTime.UtcNow,
                ContentTypeAlias = contentTypeAlias
            };

            await writer.WriteAsync(new SchemaQueueItem(dto.Id, contentTypeAlias), cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task HandleAsync(DataTypeSavedNotification notification, CancellationToken cancellationToken)
    {
        foreach (IDataType entity in notification.SavedEntities)
        {
            PagedModel<RelationItemModel> relations = await dataTypeService.GetPagedRelationsAsync(entity.Key, 0, 1000)
                .ConfigureAwait(false);

            foreach (string contentTypeAlias in relations.Items.Select(x => x.ContentTypeAlias).OfType<string>())
            {
                SchemaQueueDto dto = new()
                {
                    Id = Guid.CreateVersion7(),
                    CreatedAt = DateTime.UtcNow,
                    ContentTypeAlias = contentTypeAlias
                };

                await queueRepository.InsertAsync(dto, cancellationToken).ConfigureAwait(false);
                await writer.WriteAsync(new SchemaQueueItem(dto.Id, contentTypeAlias), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
