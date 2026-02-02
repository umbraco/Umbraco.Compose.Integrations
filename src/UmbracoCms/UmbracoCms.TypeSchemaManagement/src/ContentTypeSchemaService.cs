using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal class ContentTypeSchemaService(
    IContentTypeService contentTypeService,
    IPublishedContentTypeCache publishedContentTypeCache,
    IShortStringHelper shortStringHelper)
    : IContentTypeSchemaService
{
    public ContentTypeSchemaInfo? GetDocumentTypeByAlias(string alias)
    {
        return GetContentTypeSchemaInfo(PublishedItemType.Content, contentTypeService.Get(alias));
    }

    private ContentTypeSchemaInfo? GetContentTypeSchemaInfo(PublishedItemType itemType, IContentType? contentType)
    {
        if (contentType is null)
        {
            return null;
        }

        IPublishedContentType publishedContentType = publishedContentTypeCache.Get(itemType, contentType.Alias);
        HashSet<string> ownPropertyAliases = [.. contentType.PropertyTypes.Select(p => p.Alias),];

        return new()
        {
            Alias = contentType.Alias,
            SchemaId = GetContentTypeSchemaId(contentType.Alias),
            CompositionSchemaIds = [.. publishedContentType.CompositionAliases.Select(GetContentTypeSchemaId),],
            Properties =
            [
                ..publishedContentType.PropertyTypes.Select(p => new ContentTypePropertySchemaInfo
                {
                    Alias = p.Alias,
                    EditorAlias = p.EditorAlias,
                    DeliveryApiClrType = p.DeliveryApiModelClrType,
                    Inherited = !ownPropertyAliases.Contains(p.Alias),
                }),
            ],
            IsElement = publishedContentType.IsElement,
        };
    }

    private string GetContentTypeSchemaId(string contentTypeAlias)
    {
        return contentTypeAlias.ToCleanString(shortStringHelper, CleanStringType.ConvertCase | CleanStringType.PascalCase);
    }
}
