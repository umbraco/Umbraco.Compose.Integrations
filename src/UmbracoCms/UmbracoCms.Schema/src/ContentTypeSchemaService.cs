using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.Schema;

internal class ContentTypeSchemaService(
    IContentTypeService contentTypeService,
    IPublishedContentTypeCache publishedContentTypeCache,
    IShortStringHelper shortStringHelper)
    : IContentTypeSchemaService
{
    public ContentTypeSchemaInfo GetDocumentTypeByAlias(string alias)
        => GetContentTypeSchemaInfo(PublishedItemType.Content, contentTypeService.Get(alias));

    private ContentTypeSchemaInfo GetContentTypeSchemaInfo(PublishedItemType itemType, IContentType? contentType)
    {
        ArgumentNullException.ThrowIfNull(contentType);

        var publishedContentType = publishedContentTypeCache.Get(itemType, contentType.Alias);
        HashSet<string> ownPropertyAliases = [.. contentType.PropertyTypes.Select(p => p.Alias)];

        return new ContentTypeSchemaInfo
        {
            Alias = contentType.Alias,
            SchemaId = GetContentTypeSchemaId(contentType.Alias),
            CompositionSchemaIds = [.. publishedContentType.CompositionAliases.Select(GetContentTypeSchemaId)],
            Properties =
            [
                ..publishedContentType.PropertyTypes.Select(p => new ContentTypePropertySchemaInfo
                {
                    Alias = p.Alias,
                    EditorAlias = p.EditorAlias,
                    DeliveryApiClrType = p.DeliveryApiModelClrType,
                    Inherited = !ownPropertyAliases.Contains(p.Alias),
                })
            ],
            IsElement = publishedContentType.IsElement,
        };
    }

    private string GetContentTypeSchemaId(string contentTypeAlias) =>
        contentTypeAlias.ToCleanString(shortStringHelper, CleanStringType.ConvertCase | CleanStringType.PascalCase);
}