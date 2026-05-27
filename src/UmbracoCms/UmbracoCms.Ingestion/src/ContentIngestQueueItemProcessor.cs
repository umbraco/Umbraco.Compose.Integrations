using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal sealed class ContentIngestQueueItemProcessor(
    IApiContentBuilder apiContentBuilder,
    IDocumentNavigationQueryService navigationQueryService,
    ILanguageService languageService,
    IPublishedContentStatusFilteringService publishedStatusFilteringService,
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
                foreach (UpsertContentEntry processedItem in ProcessItem(updated, content, culture, includeChildren))
                {
                    yield return processedItem;
                }
            }
        }
    }

    private IEnumerable<UpsertContentEntry> ProcessItem(
        HashSet<string> updated,
        IPublishedContent content,
        string culture,
        bool includeChildren)
    {
        if (updated.Contains($"{content.Key}_{culture}"))
        {
            yield break;
        }

        updated.Add($"{content.Key}_{culture}");

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
            yield break;
        }

        navigationQueryService.TryGetParentKey(content.Key, out Guid? parentId);
        navigationQueryService.TryGetAncestorsKeys(content.Key, out IEnumerable<Guid> ancestors);

        yield return new()
        {
            Data = new(apiContent, parentId, [.. ancestors]),
            Id = content.Key.ToString(),
            Type = content.ContentType.Alias,
            Variant = !content.ContentType.VariesByCulture() || string.IsNullOrEmpty(culture) || culture == "*" ? null : culture
        };

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

            foreach (UpsertContentEntry processedChild in ProcessItem(updated, child, culture, true))
            {
                yield return processedChild;
            }
        }
    }
}
