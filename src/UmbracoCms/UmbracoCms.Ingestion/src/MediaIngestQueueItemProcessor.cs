using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal sealed partial class MediaIngestQueueItemProcessor(
    IApiMediaBuilder apiMediaBuilder,
    IMediaNavigationQueryService navigationQueryService,
    IHttpContextAccessor httpContextAccessor,
    IPublishedMediaStatusFilteringService publishedStatusFilteringService,
    IServiceProvider serviceProvider,
    IUmbracoContextAccessor umbracoContextAccessor,
    IUmbracoContextFactory umbracoContextFactory,
    ILogger<MediaIngestQueueItemProcessor> logger) : IIngestQueueItemProcessor<MediaIngestQueueItem>
{
    public IAsyncEnumerable<IngestEntry> ProcessAsync(MediaIngestQueueItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        return ProcessAsyncCoreAsync(item, cancellationToken);
    }

    private async IAsyncEnumerable<IngestEntry> ProcessAsyncCoreAsync(MediaIngestQueueItem item, [EnumeratorCancellation] CancellationToken cancellationToken)
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
            cancellationToken.ThrowIfCancellationRequested();

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

            httpContextAccessor.HttpContext = httpContext;
            umbracoContextAccessor.Set(context.UmbracoContext);

            HashSet<Guid> processed = [];
            Queue<Guid> queue = new([entity.Id]);

            while (queue.TryDequeue(out Guid id))
            {
                cancellationToken.ThrowIfCancellationRequested();

                IPublishedContent? media = await context.UmbracoContext.Media.GetByIdAsync(id).ConfigureAwait(false);

                if (media is null)
                {
                    LogMediaNotFound(entity.Id);
                    continue;
                }

                if (!processed.Add(media.Key))
                {
                    continue;
                }

                IApiMedia? apiMedia = apiMediaBuilder.Build(media);

                if (apiMedia is null)
                {
                    LogNoAPIContentWasBuilt(media.Name, media.Key);
                    continue;
                }

                navigationQueryService.TryGetParentKey(media.Key, out Guid? parentId);
                navigationQueryService.TryGetAncestorsKeys(media.Key, out IEnumerable<Guid> ancestors);

                yield return new UpsertMediaEntry
                {
                    Data = new(apiMedia, parentId, [.. ancestors]),
                    Id = media.Key.ToString(),
                    Type = media.ContentType.Alias
                };

                if (entity.ChangeType is not MediaChangeType.UpdateWithDescendants)
                {
                    continue;
                }

                IEnumerable<IPublishedContent> children = media.Children<IPublishedContent>(
                    navigationQueryService,
                    publishedStatusFilteringService);

                foreach (IPublishedContent child in children)
                {
                    queue.Enqueue(child.Key);
                }
            }
        }
    }

    [LoggerMessage(LogLevel.Debug, "Processing entry {Entity}")]
    private partial void LogProcessingEntity(MediaChangePayload entity);

    [LoggerMessage(LogLevel.Warning, "Could not get media with id {Id} from the Published Media Cache")]
    private partial void LogMediaNotFound(Guid id);

    [LoggerMessage(LogLevel.Warning, "No API Content was built for Media item '{Name}', '{Id}'")]
    private partial void LogNoAPIContentWasBuilt(string name, Guid id);
}
