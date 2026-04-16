using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal sealed class UmbracoContentIngestItemQueueProcessor(
    IApiContentBuilder apiContentBuilder,
    IDocumentNavigationQueryService navigationQueryService,
    IPublishedContentStatusFilteringService publishedStatusFilteringService,
    IUmbracoContextAccessor umbracoContextAccessor,
    IUmbracoContextFactory umbracoContextFactory,
    IVariationContextAccessor variationContextAccessor,
    ILogger<UmbracoContentIngestItemQueueProcessor> logger) : IIngestQueueItemProcessor<ContentIngestQueueItem>
{
    private readonly IUmbracoContextFactory _umbracoContextFactory = umbracoContextFactory;
    private readonly IVariationContextAccessor _variationContextAccessor = variationContextAccessor;
    private readonly ILogger<UmbracoContentIngestItemQueueProcessor> _logger = logger;
    private readonly IApiContentBuilder _apiContentBuilder = apiContentBuilder;
    private readonly IPublishedContentStatusFilteringService _publishedStatusFilteringService = publishedStatusFilteringService;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor = umbracoContextAccessor;
    private readonly IDocumentNavigationQueryService _navigationQueryService = navigationQueryService;

    public IAsyncEnumerable<IngestEntry> ProcessAsync(ContentIngestQueueItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        return ProcessAsyncCoreAsync(item);
    }

    private async IAsyncEnumerable<IngestEntry> ProcessAsyncCoreAsync(ContentIngestQueueItem item)
    {
        foreach (ContentChangePayload entity in item.Entities)
        {
            _logger.LogDebug("Processing entry {Entity}", entity);

            if (entity is { ChangeType: ContentChangeType.Delete, })
            {
                if (entity.AffectedCultures is { Count: > 0, })
                {
                    foreach (string culture in entity.AffectedCultures)
                    {
                        yield return new DeleteEntry { Id = entity.Id.ToString(), Variant = culture, };
                    }
                }
                else
                {
                    // hmm... we somehow need to delete all items with the specified id both for all cultures, and invariant
                    // maybe JsonPath stuff wil help us
                    yield return new DeleteEntry { Id = entity.Id.ToString(), };
                }
                continue;
            }

            using Cms.Core.UmbracoContextReference context = _umbracoContextFactory.EnsureUmbracoContext();
            IPublishedContent? content = await context.UmbracoContext.Content.GetByIdAsync(entity.Id).ConfigureAwait(false);

            if (content is null)
            {
                _logger.LogWarning("Could not get content with id {Id} from the Published Content Cache", entity.Id);
                continue;
            }

            foreach (string culture in entity.AffectedCultures is { Count: > 0, }
                ? entity.AffectedCultures
                : content.Cultures.Select(static x => x.Value.Culture))
            {
                if (!content.IsPublished(culture))
                {
                    _logger.LogWarning("Got unpublished content from cache");
                    continue;
                }

                _variationContextAccessor.VariationContext = new(culture);
                _umbracoContextAccessor.Set(context.UmbracoContext);

                foreach (UpsertContentEntry processedItem in ProcessItem(
                    content,
                    culture,
                    entity.ChangeType is ContentChangeType.UpdateWithDescendants))
                {
                    yield return processedItem;
                }
            }
        }
    }

    private IEnumerable<UpsertContentEntry> ProcessItem(IPublishedContent content, string culture, bool includeChildren)
    {
        Cms.Core.Models.DeliveryApi.IApiContent? apiContent = _apiContentBuilder.Build(content);

        if (apiContent is null)
        {
            _logger.LogWarning(
                "No API Content was built for item '{Name}', '{Culture}', '{Id}'",
                content.Name(_variationContextAccessor, culture),
                culture,
                content.Key);
            yield break;
        }
        Guid[] ancestors =
            [.. content.Ancestors<IPublishedContent>(_navigationQueryService, _publishedStatusFilteringService)
                .Select(static x => x.Key),];

        yield return new()
        {
            Data = new(
                apiContent,
                culture,
                content.Parent<IPublishedContent>(_navigationQueryService, _publishedStatusFilteringService)?.Key,
                ancestors),
            Id = content.Key.ToString(),
            Type = content.ContentType.Alias,
            Variant = string.IsNullOrEmpty(culture) ? null : culture,
        };

        if (!includeChildren)
        {
            yield break;
        }

        foreach (IPublishedContent child in content.Children<IPublishedContent>(_navigationQueryService, _publishedStatusFilteringService))
        {
            foreach (UpsertContentEntry processedChild in ProcessItem(child, culture, includeChildren))
            {
                yield return processedChild;
            }
        }
    }
}
