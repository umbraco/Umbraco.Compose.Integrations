using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Persistence;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal sealed class ContentTypeNotificationHandler(
    ChannelWriter<SchemaQueueItem> writer,
    ISchemaQueueRepository queueRepository,
    IContentTypeService contentTypeService,
    IDataTypeService dataTypeService,
    ICoreScopeProvider coreScopeProvider,
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

        await DeferredActions.ExecuteDeferredAsync(
            coreScopeProvider,
            async () =>
            {
                foreach (string contentTypeAlias in notification.SavedEntities.Select(contentType => contentType.Alias))
                {
                    SchemaQueueDto dto = new()
                    {
                        Id = Guid.NewGuid(),
                        CreatedAt = DateTime.UtcNow,
                        ContentTypeAlias = contentTypeAlias
                    };

                    await queueRepository.InsertAsync(dto, cancellationToken).ConfigureAwait(false);
                    await writer.WriteAsync(new SchemaQueueItem(dto.Id, contentTypeAlias), cancellationToken).ConfigureAwait(false);
                }
            })
            .ConfigureAwait(false);
    }

    public async Task HandleAsync(DataTypeSavedNotification notification, CancellationToken cancellationToken)
    {
        if (!options.Value.IsValid)
        {
            logger.LogDebug("Skipping type schema update - Compose options are not valid.");
            return;
        }

        await DeferredActions.ExecuteDeferredAsync(
            coreScopeProvider,
            async () =>
            {
                foreach (IDataType entity in notification.SavedEntities)
                {
                    IReadOnlyDictionary<Udi, IEnumerable<string>> relations = dataTypeService.GetReferences(entity.Id);

                    IEnumerable<IContentType> contentTypes = contentTypeService.GetAll(relations.Select(x => ((GuidUdi) x.Key).Guid));

                    foreach (string contentTypeAlias in contentTypes.Select(x => x.Alias).OfType<string>())
                    {
                        SchemaQueueDto dto = new()
                        {
                            Id = Guid.NewGuid(),
                            CreatedAt = DateTime.UtcNow,
                            ContentTypeAlias = contentTypeAlias
                        };

                        await queueRepository.InsertAsync(dto, cancellationToken).ConfigureAwait(false);
                        await writer.WriteAsync(new SchemaQueueItem(dto.Id, contentTypeAlias), cancellationToken).ConfigureAwait(false);
                    }
                }
            })
            .ConfigureAwait(false);
    }
}
