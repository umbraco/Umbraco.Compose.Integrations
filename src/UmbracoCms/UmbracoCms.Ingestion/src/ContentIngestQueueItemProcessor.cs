using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal sealed class ContentIngestQueueItemProcessor(
    IApiContentBuilder apiContentBuilder,
    IDocumentNavigationQueryService navigationQueryService,
    ILanguageService languageService,
    IPublishedContentStatusFilteringService publishedStatusFilteringService,
    ISegmentService segmentService,
    IUmbracoContextAccessor umbracoContextAccessor,
    IUmbracoContextFactory umbracoContextFactory,
    IVariationContextAccessor variationContextAccessor,
    ILogger<ContentIngestQueueItemProcessor> logger) : IIngestQueueItemProcessor<ContentIngestQueueItem>
{
    public IAsyncEnumerable<IngestEntry> ProcessAsync(ContentIngestQueueItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        return ProcessAsyncCoreAsync(item);
    }

    private async IAsyncEnumerable<IngestEntry> ProcessAsyncCoreAsync(ContentIngestQueueItem item)
    {
        HashSet<string> updated = [];
        HashSet<string> deleted = [];

        foreach (ContentChangePayload entity in item.Entities)
        {
            logger.LogDebug("Processing entry {Entity}", entity);

            if (entity is { ChangeType: ContentChangeType.Delete })
            {
                string entityId = entity.Id.ToString();

                if (entity.AffectedCultures is { Count: > 0 })
                {
                    foreach (string culture in entity.AffectedCultures)
                    {
                        if (culture == "*")
                        {
                            if (deleted.Contains(entityId))
                            {
                                continue;
                            }

                            deleted.Add(entityId);

                            yield return new DeleteEntry { Id = entityId };
                            yield return new DeleteWhereEntry
                            {
                                Where = new()
                                {
                                    { "ancestors_some", new string[] { entityId } },
                                    { "variant", null }
                                }
                            };
                        }
                        else
                        {
                            if (deleted.Contains($"{entityId}_{culture}"))
                            {
                                continue;
                            }

                            deleted.Add($"{entityId}_{culture}");

                            yield return new DeleteEntry { Id = entityId, Variant = culture };
                            yield return new DeleteWhereEntry
                            {
                                Where = new()
                                {
                                    { "ancestors_some", new string[] { entityId } },
                                    { "variant", culture }
                                }
                            };
                        }
                    }
                }
                else
                {
                    if (deleted.Contains(entityId))
                    {
                        continue;
                    }

                    deleted.Add(entityId);

                    yield return new DeleteEntry { Id = entityId };
                    yield return new DeleteWhereEntry
                    {
                        Where = new()
                            {
                                { "ancestors_some", new string[] { entityId } },
                                { "variant", null }
                            }
                    };

                    foreach (ILanguage language in await languageService.GetAllAsync().ConfigureAwait(false))
                    {
                        string? culture = language.CultureInfo?.Name;
                        if (culture is null || deleted.Contains($"{entityId}_{culture}"))
                        {
                            continue;
                        }

                        deleted.Add($"{entityId}_{culture}");

                        yield return new DeleteWhereEntry
                        {
                            Where = new()
                                {
                                    { "ancestors_some", new string[] { entityId } },
                                    { "variant", culture }
                                }
                        };
                    }
                }
                continue;
            }

            using UmbracoContextReference context = umbracoContextFactory.EnsureUmbracoContext();
            IPublishedContent? content = await context.UmbracoContext.Content.GetByIdAsync(entity.Id).ConfigureAwait(false);

            if (content is null)
            {
                logger.LogWarning("Could not get content with id {Id} from the Published Content Cache", entity.Id);
                continue;
            }

            string[] cultures = [.. entity.AffectedCultures is { Count: > 0 }
                ? entity.AffectedCultures
                : content.Cultures.Select(static x => x.Value.Culture)];

            foreach (string culture in cultures)
            {
                if (!content.IsPublished(culture))
                {
                    logger.LogWarning("Got unpublished content from cache");
                    continue;
                }

                bool includeChildren = entity.ChangeType is ContentChangeType.UpdateWithDescendants;
                await foreach (UpsertContentEntry processedItem in ProcessItemAsync(updated, content, culture, includeChildren))
                {
                    yield return processedItem;
                }
            }
        }
    }

    private async IAsyncEnumerable<UpsertContentEntry> ProcessItemAsync(
        HashSet<string> updated,
        IPublishedContent content,
        string culture,
        bool includeChildren)
    {
        if (content.ContentType.VariesBySegment())
        {
            Attempt<PagedModel<Segment>?, SegmentOperationStatus> segments = await segmentService.GetPagedSegmentsForDocumentAsync(content.Key, 0, 9999);

            foreach (Segment segment in segments.Result.Items)
            {
                UpsertContentEntry? segmentEntry = ProcessItem(updated, content, culture, segment.Alias);
                if (segmentEntry is not null)
                {
                    yield return segmentEntry;
                }
            }
        }

        UpsertContentEntry? entry = ProcessItem(updated, content, culture, null);
        if (entry is null)
        {
            yield break;
        }

        yield return entry;

        if (!includeChildren)
        {
            yield break;
        }

        IEnumerable<IPublishedContent> children = content.Children<IPublishedContent>(
            navigationQueryService,
            publishedStatusFilteringService,
            culture);

        foreach (IPublishedContent child in children)
        {
            if (!child.IsPublished(culture))
            {
                continue;
            }

            await foreach (UpsertContentEntry processedChild in ProcessItemAsync(updated, child, culture, true))
            {
                yield return processedChild;
            }
        }
    }

    private UpsertContentEntry? ProcessItem(
        HashSet<string> updated,
        IPublishedContent content,
        string? culture,
        string? segment)
    {
        if (updated.Contains($"{content.Key}_{culture}_{segment}"))
        {
            return null;
        }

        updated.Add($"{content.Key}_{culture}_{segment}");

        using UmbracoContextReference context = umbracoContextFactory.EnsureUmbracoContext();

        variationContextAccessor.VariationContext = new(culture);
        umbracoContextAccessor.Set(context.UmbracoContext);

        IApiContent? apiContent = apiContentBuilder.Build(content);

        if (apiContent is null)
        {
            logger.LogWarning(
                "No API Content was built for item '{Name}', '{Culture}', '{Id}'",
                content.Name(variationContextAccessor, culture),
                culture,
                content.Key);
            return null;
        }

        navigationQueryService.TryGetParentKey(content.Key, out Guid? parentId);
        navigationQueryService.TryGetAncestorsKeys(content.Key, out IEnumerable<Guid> ancestors);

        string? variant =
            content.ContentType.VariesByCultureAndSegment() && segment is not null ? $"{culture}/{segment}" :
            content.ContentType.VariesBySegment() && segment is not null  ? segment :
            content.ContentType.VariesByCulture() && culture is not null ? culture :
            null;

        return new()
        {
            Data = new(apiContent, parentId, [.. ancestors]),
            Id = content.Key.ToString(),
            Type = content.ContentType.Alias,
            Variant = variant
        };
    }
}
