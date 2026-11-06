using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal sealed partial class MediaIngestQueueItemProcessor(
    IApiMediaBuilder apiMediaBuilder,
    IMediaNavigationQueryService navigationQueryService,
    IHttpContextAccessor httpContextAccessor,
    IServiceProvider serviceProvider,
    IUmbracoContextAccessor umbracoContextAccessor,
    IUmbracoContextFactory umbracoContextFactory,
    ILogger<MediaIngestQueueItemProcessor> logger) : IIngestQueueItemProcessor<MediaIngestQueueItem>
{
    public IAsyncEnumerable<IngestEntry> ProcessAsync(MediaIngestQueueItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        return ProcessAsyncCoreAsync(item);
    }

    private async IAsyncEnumerable<IngestEntry> ProcessAsyncCoreAsync(MediaIngestQueueItem item)
    {
        HttpContext httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
            Request =
            {
                Host = new("localhost"),
                Scheme = "http"
            }
        };

        foreach (MediaChangePayload entity in item.Entities)
        {
            LogProcessingEntity(entity);

            using UmbracoContextReference context = umbracoContextFactory.EnsureUmbracoContext();

            if (entity is { ChangeType: MediaChangeType.Delete })
            {
                string entityId = entity.Id.ToString();
                yield return new DeleteEntry { Id = entityId };
                yield return new DeleteWhereEntry
                {
                    Where = new()
                    {
                        { "ancestors_some", new string[] { entityId } }
                    }
                };
                continue;
            }

            IPublishedContent? media = await context.UmbracoContext.Media.GetByIdAsync(entity.Id).ConfigureAwait(false);

            if (media is null)
            {
                LogMediaNotFound(entity.Id);
                continue;
            }

            httpContextAccessor.HttpContext = httpContext;
            umbracoContextAccessor.Set(context.UmbracoContext);

            IApiMedia? apiMedia = apiMediaBuilder.Build(media);

            if (apiMedia is null)
            {
                LogNoAPIContentWasBuilt(media.Name, media.Key);

                yield break;
            }

            navigationQueryService.TryGetParentKey(media.Key, out Guid? parentId);
            navigationQueryService.TryGetAncestorsKeys(media.Key, out IEnumerable<Guid> ancestors);

            yield return new UpsertMediaEntry
            {
                Data = new(apiMedia, parentId, [.. ancestors]),
                Id = media.Key.ToString(),
                Type = media.ContentType.Alias
            };
        }
    }

    [LoggerMessage(LogLevel.Debug, "Processing entry {Entity}")]
    private partial void LogProcessingEntity(MediaChangePayload entity);

    [LoggerMessage(LogLevel.Warning, "Could not get media with id {Id} from the Published Media Cache")]
    private partial void LogMediaNotFound(Guid id);

    [LoggerMessage(LogLevel.Warning, "No API Content was built for Media item '{Name}', '{Id}'")]
    private partial void LogNoAPIContentWasBuilt(string name, Guid id);
}
