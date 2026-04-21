using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal class JsonSchemaExporterService
{
#pragma warning disable S1075 // refactor you code to not use hardcoded absolute paths or URIs.
    private const string ComposeNodeUrl = "https://umbracocompose.com/v1/node";
#pragma warning restore S1075 // refactor you code to not use hardcoded absolute paths or URIs.

    private readonly IContentTypeService _contentTypeService;
    private readonly IPublishedContentTypeCache _publishedContentTypeCache;
    private readonly PropertySchemaResolverCollection _propertySchemaResolvers;
    private readonly JsonSchemaGeneratorOptions _jsonSchemaGeneratorOptions;

    public JsonSchemaExporterService(
        IContentTypeService contentTypeService,
        IPublishedContentTypeCache publishedContentTypeCache,
        PropertySchemaResolverCollection propertySchemaResolvers,
        IOptionsSnapshot<JsonSchemaGeneratorOptions> jsonSchemaGeneratorOptions)
    {
        _contentTypeService = contentTypeService;
        _publishedContentTypeCache = publishedContentTypeCache;
        _propertySchemaResolvers = propertySchemaResolvers;

        _jsonSchemaGeneratorOptions = jsonSchemaGeneratorOptions.Get(nameof(JsonSchemaExporterService));
    }

    public IReadOnlyDictionary<string, JsonSchema> GenerateSchemas(string contentTypeAlias)
    {
        JsonSchemaGeneratorContext context = new(_jsonSchemaGeneratorOptions);

        GenerateSchemaInternal(context, contentTypeAlias);

        return context.Schemas;
    }

    private void GenerateSchemaInternal(JsonSchemaGeneratorContext context, string contentTypeAlias)
    {
        if (context.ContainsType(contentTypeAlias))
        {
            return;
        }

        IPublishedContentType contentType = _publishedContentTypeCache.Get(PublishedItemType.Content, contentTypeAlias);

        JsonSchemaBuilder builder = context
            .CreateBuilder(JsonPropertyType.Object)
            .Title(contentType.Alias);

        context.RegisterSchema(contentTypeAlias, builder.JsonSchema);

        if (_contentTypeService.GetComposedOf(contentType.Id).Any())
        {
            builder.CustomKeyword("x-abstract", true);
        }
        else if (contentType.ItemType is PublishedItemType.Content or PublishedItemType.Media or PublishedItemType.Element)
        {
            if (contentType.ItemType is PublishedItemType.Content or PublishedItemType.Media)
            {
                builder.AllOf(x => x.Ref(ComposeNodeUrl));
            }

            JsonSchema? schema = contentType.ItemType switch
            {
                PublishedItemType.Element => GenerateType<IApiElement>(context),
                PublishedItemType.Content => GenerateType<IApiContent>(context),
                PublishedItemType.Media => GenerateType<IApiMedia>(context),
                PublishedItemType.Member => null,
                PublishedItemType.Unknown => null,
                _ => null
            };

            if (schema?.TypeName is not null)
            {
                builder.AllOf(x => x.Ref(schema.TypeName));
            }

            foreach (string composition in contentType.CompositionAliases)
            {
                builder.AllOf(x => x.Ref(composition));

                GenerateSchemaInternal(context, composition);
            }
        }

        builder.Property(
            "properties",
            builder =>
            {
                builder.Type(JsonPropertyType.Object);

                foreach (PublishedPropertyType propertyType in contentType.PropertyTypes.Cast<PublishedPropertyType>())
                {
                    JsonSchema? schema;
                    if (_propertySchemaResolvers.FirstOrDefault(x => x.CanHandle(propertyType)) is { } handler)
                    {
                        schema = handler.Process(context, propertyType);
                    }
                    else
                    {
                        schema = GenerateType(context, propertyType.DeliveryApiModelClrType);
                    }

                    if (schema is not null)
                    {
                        if (schema.Type is JsonPropertyType.Object && schema.TypeName is not null)
                        {
                            builder.Property(propertyType.Alias, builder => builder.Type(JsonPropertyType.Object).Ref($"./{schema.TypeName}"));
                        }
                        else if (schema.Type is JsonPropertyType.Array && schema.Items?.TypeName is not null)
                        {
                            builder.Property(propertyType.Alias, builder => builder.Type(JsonPropertyType.Array).Items(builder => builder.Type(JsonPropertyType.Object).Ref($"./{schema.Items.TypeName}")));
                        }
                        else
                        {
                            builder.Property(propertyType.Alias, schema);
                        }
                    }
                    else
                    {
                        builder.Property(propertyType.Alias, context.Generate(propertyType.DeliveryApiModelClrType));
                    }
                }

                return builder;
            });
    }

    private static JsonSchema? GenerateType<T>(JsonSchemaGeneratorContext context) =>
        GenerateType(context, typeof(T));

    private static JsonSchema GenerateType(JsonSchemaGeneratorContext context, Type type) =>
        context.Generate(type);
}
