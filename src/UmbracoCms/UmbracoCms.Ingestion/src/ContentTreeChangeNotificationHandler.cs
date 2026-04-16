using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal sealed class ContentTreeChangeNotificationHandler(
    ILogger<ContentTreeChangeNotificationHandler> logger,
    IIngestService ingestService,
    IOptions<UmbracoComposeOptions> composeOptions,
    IOptions<UmbracoComposeIngestionOptions> ingestionOptions
) : INotificationAsyncHandler<ContentTreeChangeNotification>
{
    public async Task HandleAsync(ContentTreeChangeNotification notification, CancellationToken cancellationToken)
    {
        if (!composeOptions.Value.IsValid)
        {
            logger.LogDebug("Skipping ingestion - Compose options are not valid.");
            return;
        }

        if (!ingestionOptions.Value.IsValid)
        {
            logger.LogDebug("Skipping ingestion - Ingestion options are not valid.");
            return;
        }

        if (!notification.Changes.Any())
        {
            logger.LogDebug("Skipping ingestion - Recieved notification with zero changes.");
            return;
        }

        List<ContentChangePayload> entities =
            [..
                notification.Changes
                    .Select(change => new ContentChangePayload(
                        change.Item.Key,
                        GetChangeType(change),
                        GetAffectedCultures(change)))
            ,];

        await ingestService
            .EnqueueAsync(new ContentIngestQueueItem(entities), cancellationToken)
            .ConfigureAwait(false);
    }

    private static List<string> GetAffectedCultures(TreeChange<IContent> change) =>
        [..
            // TODO:
            // We currently assume that a notification generates only one type of change per
            // content item - i.e. a given content item will only have publishedCultures or
            // unpublishedCultures. It does look like there are probably ways to generate changes
            // that contain both. However, such cases would seem to require manually intervening
            // and setting the publish state directly on content items rather than using available
            // methods on an IContentService.
            //
            // Eventually we may want to change the channel message to include a dictionary of
            // Culture => ContentChangeType per item.
            (change.PublishedCultures ?? [])
                .Union(change.UnpublishedCultures ?? []),];

    private static ContentChangeType GetChangeType(TreeChange<IContent> change) =>
        change.ChangeTypes switch
        {
            TreeChangeTypes.Remove => ContentChangeType.Delete,
            _ when change.UnpublishedCultures?.Any() is true => ContentChangeType.Delete,
            TreeChangeTypes.RefreshBranch => ContentChangeType.UpdateWithDescendants,
            _ => ContentChangeType.Update,
        };
}
