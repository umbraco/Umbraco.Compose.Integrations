using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal class JsonSchemaExporterService(
    IContentTypeService contentTypeService,
    IMediaTypeService mediaTypeService,
    IPublishedContentTypeFactory publishedContentTypeFactory,
    PropertySchemaResolverCollection propertySchemaResolvers,
    IOptionsSnapshot<JsonSchemaGeneratorOptions> jsonSchemaGeneratorOptions,
    IOptions<UmbracoComposeOptions> options)
{
#pragma warning disable S1075 // refactor you code to not use hardcoded absolute paths or URIs.
    private const string ComposeNodeUrl = "https://umbracocompose.com/v1/node";
#pragma warning restore S1075 // refactor you code to not use hardcoded absolute paths or URIs.

    private readonly JsonSchemaGeneratorOptions _jsonSchemaGeneratorOptions =
        jsonSchemaGeneratorOptions.Get(nameof(JsonSchemaExporterService));

    public async Task<IReadOnlyDictionary<string, JsonSchema>> GenerateSchemasAsync(StringUdi udi)
    {
        JsonSchemaGeneratorContext context = new(_jsonSchemaGeneratorOptions);

        await GenerateSchemaInternalAsync(context, udi).ConfigureAwait(false);

        return context.Schemas;
    }

    private async Task GenerateSchemaInternalAsync(JsonSchemaGeneratorContext context, StringUdi udi)
    {
        IContentTypeComposition? contentType = udi.EntityType switch
        {
            Constants.UdiEntityType.DocumentType => contentTypeService.Get(udi.Id),
            Constants.UdiEntityType.MediaType => mediaTypeService.Get(udi.Id),
            _ => null
        };

        if (contentType is null)
        {
            return;
        }

        PublishedContentType publishedContentType = new(contentType, publishedContentTypeFactory);
        string alias = options.Value.GetTypeSchemaAlias(publishedContentType.ItemType, publishedContentType.Alias);

        if (context.ContainsType(alias))
        {
            return;
        }

        JsonSchemaBuilder builder = context
            .CreateBuilder(JsonPropertyType.Object)
            .Title(contentType.Name!);

        context.RegisterSchema(alias, builder.JsonSchema);

        bool isUsedAsComposition = publishedContentType.ItemType switch
        {
            PublishedItemType.Content or PublishedItemType.Element => contentTypeService.GetComposedOf(contentType.Id).Any(),
            PublishedItemType.Media => mediaTypeService.GetComposedOf(contentType.Id).Any(),
            _ => false
        };

        if (isUsedAsComposition)
        {
            builder.CustomKeyword("x-abstract", true);
        }
        else
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
                builder.AllOf(x => x.Ref(options.Value.GetTypeSchemaAlias(publishedContentType.ItemType, composition)));

                await GenerateSchemaInternalAsync(context, new (udi.EntityType, composition)).ConfigureAwait(false);
            }
        }

        // The schema builder API is synchronous, so resolve the resolver-provided schemas up front.
        Dictionary<string, JsonSchema> resolvedSchemas = [];
        foreach (PublishedPropertyType propertyType in publishedContentType.PropertyTypes.Cast<PublishedPropertyType>())
        {
            if (propertySchemaResolvers.FirstOrDefault(x => x.CanHandle(propertyType)) is { } handler)
            {
                resolvedSchemas[propertyType.Alias] = await handler.ProcessAsync(context, propertyType).ConfigureAwait(false);
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
                    if (resolvedSchemas.TryGetValue(propertyType.Alias, out JsonSchema? resolvedSchema))
                    {
                        schema = resolvedSchema;
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
