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
    private const string ComposeSchemaUrl = "https://umbracocompose.com/v1/schema";
    private const string ComposeNodeUrl = "https://umbracocompose.com/v1/node";
#pragma warning restore S1075 // refactor you code to not use hardcoded absolute paths or URIs.

    private readonly JsonSchemaGenerator _jsonSchemaGenerator;
    private readonly IContentTypeService _contentTypeService;
    private readonly IPublishedContentTypeCache _publishedContentTypeCache;
    private readonly ITypeNameGenerator _typeNameGenerator;
    private readonly PropertySchemaResolverCollection _propertySchemaResolvers;

    public JsonSchemaExporterService(
        IContentTypeService contentTypeService,
        IPublishedContentTypeCache publishedContentTypeCache,
        ITypeNameGenerator typeNameGenerator,
        PropertySchemaResolverCollection propertySchemaResolvers,
        IOptionsSnapshot<JsonSchemaGeneratorOptions> jsonSchemaGeneratorOptions)
    {
        _contentTypeService = contentTypeService;
        _publishedContentTypeCache = publishedContentTypeCache;
        _typeNameGenerator = typeNameGenerator;
        _propertySchemaResolvers = propertySchemaResolvers;

        _jsonSchemaGenerator = new(jsonSchemaGeneratorOptions.Get(nameof(JsonSchemaExporterService)));
    }

    public IReadOnlyDictionary<string, JsonSchema> GenerateSchemas(string contentTypeAlias)
    {
        Dictionary<string, JsonSchema> schemas = [];

        GenerateSchemaInternal(schemas, contentTypeAlias);

        return schemas;
    }

    private void GenerateSchemaInternal(Dictionary<string, JsonSchema> schemas, string contentTypeAlias)
    {
        if (schemas.ContainsKey(contentTypeAlias))
        {
            return;
        }

        IPublishedContentType contentType = _publishedContentTypeCache.Get(PublishedItemType.Content, contentTypeAlias);

        JsonSchemaBuilder builder = JsonSchemaBuilder
            .Create()
            .Schema(ComposeSchemaUrl)
            .Type(JsonValueType.Object)
            .Title(contentType.Alias);

        schemas.Add(contentTypeAlias, builder.Build());

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

            (string? typeName, _) = contentType.ItemType switch
            {
                PublishedItemType.Element => GenerateTypes<IApiElement>(schemas),
                PublishedItemType.Content => GenerateTypes<IApiContent>(schemas),
                PublishedItemType.Media => GenerateTypes<IApiMedia>(schemas),
                PublishedItemType.Member => (null, null),
                PublishedItemType.Unknown => (null, null),
                _ => (null, null)
            };

            if (typeName is not null)
            {
                builder.AllOf(x => x.Ref(typeName));
            }

            foreach (string composition in contentType.CompositionAliases)
            {
                builder.AllOf(x => x.Ref(composition));

                GenerateSchemaInternal(schemas, composition);
            }
        }

        builder.Property(
            "properties",
            builder =>
            {
                foreach (PublishedPropertyType propertyType in contentType.PropertyTypes.Cast<PublishedPropertyType>())
                {
                    string? typeName = null;
                    JsonSchema? schema;
                    if (_propertySchemaResolvers.FirstOrDefault(x => x.CanHandle(propertyType)) is { } handler)
                    {
                        schema = handler.Process(propertyType, _jsonSchemaGenerator);
                    }
                    else
                    {
                        (typeName, schema) = GenerateTypes(schemas, propertyType.DeliveryApiModelClrType);
                    }

                    if (schema is not null)
                    {
                        if (schema.Type is JsonValueType.Array && typeName is not null)
                        {
                            builder.Property(propertyType.Alias, builder => builder.Ref(typeName));
                        }
                        else
                        {
                            builder.Property(propertyType.Alias, schema);
                        }
                    }
                    else
                    {
                        builder.Property(propertyType.Alias, _jsonSchemaGenerator.Generate(propertyType.DeliveryApiModelClrType));
                    }
                }

                return builder;
            });
    }

    private (string TypeName, JsonSchema? RootSchema) GenerateTypes<T>(Dictionary<string, JsonSchema> schemas) =>
        GenerateTypes(schemas, typeof(T));

    private (string TypeName, JsonSchema? RootSchema) GenerateTypes(Dictionary<string, JsonSchema> schemas, Type type)
    {
        string typeName = _typeNameGenerator.GenerateName(type);

        if (!schemas.ContainsKey(typeName))
        {
            foreach ((string? name, JsonSchema? schema) in _jsonSchemaGenerator.GenerateAll(type, ComposeSchemaUrl))
            {
                schemas.TryAdd(name, schema);
            }
        }

        schemas.TryGetValue(typeName, out JsonSchema? rootSchema);

        return (typeName, rootSchema);
    }
}
