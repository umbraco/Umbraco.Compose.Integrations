using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal sealed class ContentIngestQueueItemProcessor(
    IApiContentBuilder apiContentBuilder,
    ILocalizationService localizationService,
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

                    foreach (ILanguage language in localizationService.GetAllLanguages())
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

            // The published snapshot only lives as long as the Umbraco context, which does not survive a yield
            // in this async iterator, so all entries for the entity are materialized before yielding any of them.
            foreach (UpsertContentEntry processedItem in ProcessEntity(updated, entity))
            {
                yield return processedItem;
            }
        }
    }

    private List<UpsertContentEntry> ProcessEntity(HashSet<string> updated, ContentChangePayload entity)
    {
        List<UpsertContentEntry> entries = [];

        using UmbracoContextReference context = umbracoContextFactory.EnsureUmbracoContext();
        IPublishedContent? content = context.UmbracoContext.Content!.GetById(entity.Id);

        if (content is null)
        {
            logger.LogWarning("Could not get content with id {Id} from the Published Content Cache", entity.Id);
            return entries;
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
            entries.AddRange(ProcessItem(updated, content, culture, includeChildren));
        }

        return entries;
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

        Guid? parentId = content.Parent?.Key;
        IEnumerable<Guid> ancestors = content.Ancestors().Select(x => x.Key);

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
            variationContextAccessor,
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
