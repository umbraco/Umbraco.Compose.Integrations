using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Compose.Integrations.UmbracoCms.Core;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal class ContentTypeNotificationHandler(
    ChannelWriter<SchemaQueueItem> writer,
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

        await ExecuteDeferredAsync(async () =>
        {
            foreach (IContentType contentType in notification.SavedEntities)
            {
                await writer.WriteAsync(new SchemaQueueItem(contentType.Alias), cancellationToken).ConfigureAwait(false);
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

        await ExecuteDeferredAsync(async () =>
        {
            foreach (IDataType entity in notification.SavedEntities)
            {
                PagedModel<RelationItemModel> relations = await dataTypeService.GetPagedRelationsAsync(entity.Key, 0, 1000)
                    .ConfigureAwait(false);
                foreach (string contentTypeAlias in relations.Items.Select(x => x.ContentTypeAlias).OfType<string>())
                {
                    await writer.WriteAsync(new SchemaQueueItem(contentTypeAlias), cancellationToken).ConfigureAwait(false);
                }
            }
        })
            .ConfigureAwait(false);
    }

    private ValueTask ExecuteDeferredAsync(Func<ValueTask> action)
    {
        DeferredActions? actions = DeferredActions.Get(coreScopeProvider);
        if (actions is not null)
        {
            actions.Add(action);
        }
        else
        {
            return action();
        }

        return default;
    }
}
