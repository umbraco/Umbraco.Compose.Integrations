using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal class JsonSchemaExporterService(
    IContentTypeService contentTypeService,
    IPublishedContentTypeFactory publishedContentTypeFactory,
    PropertySchemaResolverCollection propertySchemaResolvers,
    IOptionsSnapshot<JsonSchemaGeneratorOptions> jsonSchemaGeneratorOptions)
{
#pragma warning disable S1075 // refactor you code to not use hardcoded absolute paths or URIs.
    private const string ComposeNodeUrl = "https://umbracocompose.com/v1/node";
#pragma warning restore S1075 // refactor you code to not use hardcoded absolute paths or URIs.

    private readonly JsonSchemaGeneratorOptions _jsonSchemaGeneratorOptions =
        jsonSchemaGeneratorOptions.Get(nameof(JsonSchemaExporterService));

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

        IContentType? contentType = contentTypeService.Get(contentTypeAlias);
        if (contentType is null)
        {
            return;
        }

        PublishedContentType publishedContentType = new(contentType, publishedContentTypeFactory);

        JsonSchemaBuilder builder = context
            .CreateBuilder(JsonPropertyType.Object)
            .Title(contentType.Alias);

        context.RegisterSchema(contentTypeAlias, builder.JsonSchema);

        if (contentTypeService.GetComposedOf(contentType.Id).Any())
        {
            builder.CustomKeyword("x-abstract", true);
        }
        else if (publishedContentType.ItemType is PublishedItemType.Content or PublishedItemType.Media or PublishedItemType.Element)
        {
            if (publishedContentType.ItemType is PublishedItemType.Content or PublishedItemType.Media)
            {
                builder.AllOf(x => x.Ref(ComposeNodeUrl));
            }

            JsonSchema? schema = publishedContentType.ItemType switch
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

            foreach (string composition in publishedContentType.CompositionAliases)
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

                foreach (PublishedPropertyType propertyType in publishedContentType.PropertyTypes.Cast<PublishedPropertyType>())
                {
                    JsonSchema? schema;
                    if (propertySchemaResolvers.FirstOrDefault(x => x.CanHandle(propertyType)) is { } handler)
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
                            builder.Property(
                                propertyType.Alias,
                                builder =>
                                {
                                    builder.Type(JsonPropertyType.Object).Ref($"./{schema.TypeName}");

                                    if (IsNodeType(schema.ClrType))
                                    {
                                        builder.CustomKeyword("$delivery", builder => builder.CustomKeyword("refCollection", true));
                                    }

                                    return builder;
                                });
                        }
                        else if (schema.Type is JsonPropertyType.Array && schema.Items?.TypeName is not null)
                        {
                            builder.Property(
                                propertyType.Alias,
                                builder => builder.Type(JsonPropertyType.Array)
                                    .Items(builder =>
                                    {
                                        builder.Type(JsonPropertyType.Object).Ref($"./{schema.Items.TypeName}");

                                        if (IsNodeType(schema.Items.ClrType))
                                        {
                                            builder.CustomKeyword("$delivery", builder => builder.CustomKeyword("refCollection", true));
                                        }

                                        return builder;
                                    }));
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

    private static bool IsNodeType(Type? type) =>
        type is not null &&
            (typeof(IApiContent).IsAssignableFrom(type) ||
                typeof(IApiMedia).IsAssignableFrom(type));
}
