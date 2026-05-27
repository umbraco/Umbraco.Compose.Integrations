using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal class ContentNotificationHandler(
    ICoreScopeProvider coreScopeProvider,
    IIngestService ingestService,
    ILanguageService languageService,
    IOptions<UmbracoComposeOptions> composeOptions,
    IOptions<UmbracoComposeIngestionOptions> ingestionOptions,
    ILogger<ContentNotificationHandler> logger
    ) :
        INotificationAsyncHandler<ContentPublishedNotification>,
        INotificationAsyncHandler<ContentUnpublishedNotification>,
        INotificationAsyncHandler<ContentDeletedNotification>,
        INotificationAsyncHandler<ContentMovedNotification>,
        INotificationAsyncHandler<ContentMovedToRecycleBinNotification>
{
    public async Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken)
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

        // We sometimes get unpublished entities here, discard them.
        IContent[] publishedEntities = [.. notification.PublishedEntities.Where(entity => entity.Published)];

        if (publishedEntities.Length == 0)
        {
            return;
        }

        IEnumerable<ILanguage> languages = await languageService.GetAllAsync().ConfigureAwait(false);
        IEnumerable<string> allCultures = languages.Select(x => x.CultureInfo)
            .OfType<CultureInfo>()
            .Select(x => x.Name);

        List<ContentChangePayload> payloads = [];

        foreach (IContent entity in publishedEntities)
        {
            if (entity.ContentType.VariesByCulture())
            {
                string[] publishedCultures = [.. entity.AvailableCultures
                    .Where(culture => notification.HasPublishedCulture(entity, culture))];

                // Unpublishing any culture that isn't the last published one will trigger a
                // ContentPublishedNotification; we need to handle this case.
                string[] unpublishedCultures = [.. entity.AvailableCultures
                    .Where(culture => notification.HasUnpublishedCulture(entity, culture))];

                if (publishedCultures.Length > 0)
                {
                    foreach (string culture in publishedCultures)
                    {
                        // Content that was not previously published should also cause descendants to be updated.
                        bool cultureWasPreviouslyUnpublished = entity.WasPropertyDirty(
                            Content.ChangeTrackingPrefix.PublishedCulture + culture);

                        // Publishes of variants should also update descendants. The only
                        // thing that shouldn't is re-publishes.
                        //
                        // TODO: There are some cases in which we want to send UpdateWithDescendants where
                        // we presently only send an Update. For example, if changed properties affected
                        // the route.
                        bool includeDescendants = notification.IncludeDescendants || cultureWasPreviouslyUnpublished;

                        ContentChangeType changeType = includeDescendants ?
                                            ContentChangeType.UpdateWithDescendants :
                                            ContentChangeType.Update;

                        // only include invariant if the whole content item wasn't published before
                        string[] cultures = includeDescendants && entity.WasPropertyDirty(nameof(IContent.Published))
                            ? [culture, "*"]
                            : [culture];

                        payloads.Add(new(entity.Key, changeType, cultures));
                    }
                }

                // unpublish should delete from Compose
                if (unpublishedCultures.Length > 0)
                {
                    payloads.Add(new(entity.Key, ContentChangeType.Delete, unpublishedCultures));
                }
            }
            else
            {
                bool includeDescendants = notification.IncludeDescendants || entity.WasPropertyDirty(nameof(IContent.Published));

                ContentChangeType changeType = includeDescendants
                    ? ContentChangeType.UpdateWithDescendants
                    : ContentChangeType.Update;

                string[] cultures = includeDescendants ? ["*", .. allCultures] : ["*"];

                payloads.Add(new(entity.Key, changeType, cultures));
            }
        }

        await EnqueueAsync(payloads, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(ContentUnpublishedNotification notification, CancellationToken cancellationToken)
    {
        ContentChangePayload[] payloads = await GetDeletePayloadAsync(notification.UnpublishedEntities).ConfigureAwait(false);

        await EnqueueAsync(payloads, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(ContentDeletedNotification notification, CancellationToken cancellationToken)
    {
        ContentChangePayload[] payloads = await GetDeletePayloadAsync(notification.DeletedEntities).ConfigureAwait(false);

        await EnqueueAsync(payloads, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(ContentMovedNotification notification, CancellationToken cancellationToken)
    {
        ContentChangePayload[] payloads = [.. notification.MoveInfoCollection.Select(move =>
            new ContentChangePayload(move.Entity.Key, ContentChangeType.UpdateWithDescendants, []))];

        await EnqueueAsync(payloads, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(ContentMovedToRecycleBinNotification notification, CancellationToken cancellationToken)
    {
        ContentChangePayload[] payloads = await GetDeletePayloadAsync(notification.MoveInfoCollection.Select(x => x.Entity))
            .ConfigureAwait(false);

        await EnqueueAsync(payloads, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ContentChangePayload[]> GetDeletePayloadAsync(IEnumerable<IContent> contents)
    {
        IEnumerable<ILanguage> languages = await languageService.GetAllAsync().ConfigureAwait(false);
        List<string> cultures = ["*", ..languages.Select(x => x.CultureInfo)
            .OfType<CultureInfo>()
            .Select(x => x.Name)];

        return [.. contents.Select(content => new ContentChangePayload(content.Key, ContentChangeType.Delete, cultures))];
    }

    private async Task EnqueueAsync(IReadOnlyCollection<ContentChangePayload> payloads, CancellationToken cancellationToken)
    {
        await DeferredActions.ExecuteDeferredAsync(
            coreScopeProvider,
            () => ingestService.EnqueueAsync(new ContentIngestQueueItem(payloads), cancellationToken))
            .ConfigureAwait(false);
    }
}
