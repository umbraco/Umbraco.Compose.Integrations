using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Persistence;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal sealed partial class MediaTypeNotificationHandler(
    ChannelWriter<SchemaQueueItem> writer,
    ISchemaQueueRepository queueRepository,
    ICoreScopeProvider coreScopeProvider,
    ILogger<MediaTypeNotificationHandler> logger,
    IOptions<UmbracoComposeOptions> options) :
        INotificationAsyncHandler<MediaTypeSavedNotification>
{
    public async Task HandleAsync(MediaTypeSavedNotification notification, CancellationToken cancellationToken)
    {
        if (!options.Value.IsValid)
        {
            LogConfigurationNotValid();
            return;
        }

        await DeferredActions.ExecuteDeferredAsync(
            coreScopeProvider,
            async () =>
            {
                foreach (string contentTypeAlias in notification.SavedEntities.Select(contentType => contentType.Alias))
                {
                    StringUdi udi = new(Constants.UdiEntityType.MediaType, contentTypeAlias);

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

    [LoggerMessage(LogLevel.Debug, "Skipping type schema update - Compose options are not valid")]
    private partial void LogConfigurationNotValid();
}
