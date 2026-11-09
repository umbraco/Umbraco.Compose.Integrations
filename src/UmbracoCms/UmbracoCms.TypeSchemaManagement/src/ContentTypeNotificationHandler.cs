using System.Runtime.CompilerServices;
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
    IEntityService entityService,
    ISchemaQueueRepository queueRepository,
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
                    StringUdi udi = new(Constants.UdiEntityType.DocumentType, contentTypeAlias);

                    SchemaQueueDto dto = new()
                    {
                        Id = Guid.CreateVersion7(),
                        CreatedAt = DateTime.UtcNow,
                        ContentTypeAlias = udi.ToString()
                    };

                    await queueRepository.InsertAsync(dto, cancellationToken).ConfigureAwait(false);
                    await writer.WriteAsync(new SchemaQueueItem(dto.Id, udi), cancellationToken).ConfigureAwait(false);
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
                async IAsyncEnumerable<RelationItemModel> GetAllRelationsAsync(
                    IDataType entity,
                    [EnumeratorCancellation] CancellationToken cancellationToken)
                {
                    const int PageSize = 1000;

                    int page = 0;

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        PagedModel<RelationItemModel> relations = await dataTypeService
                            .GetPagedRelationsAsync(entity.Key, page * PageSize, PageSize)
                            .ConfigureAwait(false);

                        foreach (RelationItemModel relation in relations.Items)
                        {
                            yield return relation;
                        }

                        ++page;

                        if (relations.Total <= page * PageSize)
                        {
                            yield break;
                        }
                    }
                }
                foreach (IDataType entity in notification.SavedEntities)
                {
                    await foreach (RelationItemModel relation in GetAllRelationsAsync(entity, cancellationToken))
                    {
                        if (relation.ContentTypeAlias is null)
                        {
                            continue;
                        }

                        UmbracoObjectTypes? entityType = entityService.GetObjectType(relation.ContentTypeKey);

                        string? udiType = entityType switch
                        {
                            UmbracoObjectTypes.DocumentType => Constants.UdiEntityType.DocumentType,
                            UmbracoObjectTypes.MediaType => Constants.UdiEntityType.MediaType,
                            _ => null
                        };

                        if (udiType is null)
                        {
                            continue;
                        }

                        StringUdi udi = new(udiType, relation.ContentTypeAlias);

                        SchemaQueueDto dto = new()
                        {
                            Id = Guid.CreateVersion7(),
                            CreatedAt = DateTime.UtcNow,
                            ContentTypeAlias = udi.ToString()
                        };

                        await queueRepository.InsertAsync(dto, cancellationToken).ConfigureAwait(false);
                        await writer.WriteAsync(new SchemaQueueItem(dto.Id, udi), cancellationToken).ConfigureAwait(false);
                    }
                }
            })
            .ConfigureAwait(false);
    }
}
