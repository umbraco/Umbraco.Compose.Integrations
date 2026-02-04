using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NJsonSchema;
using NJsonSchema.Generation;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.DeliveryApi;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal class JsonSchemaExporterService(
    IContentTypeSchemaService contentTypeSchemaService,
    ISchemaIdSelector schemaIdSelector,
    IOptionsMonitor<JsonOptions> options)
{
#pragma warning disable S1075 // refactor you code to not use hardcoded absolute paths or URIs.
    private const string ComposeSchemaUrl = "https://umbracocompose.com/v1/schema";
    private const string ComposeNodeUrl = "https://umbracocompose.com/v1/node";
#pragma warning restore S1075 // refactor you code to not use hardcoded absolute paths or URIs.

    private readonly JsonSerializerOptions _jsonSerializerOptions =
        options.Get(Constants.JsonOptionsNames.DeliveryApi).JsonSerializerOptions;

    public JsonElement? GenerateSchema(string contentTypeAlias)
    {
        ContentTypeSchemaInfo? contentTypeInfo = contentTypeSchemaService.GetDocumentTypeByAlias(contentTypeAlias);
        if (contentTypeInfo is null)
        {
            return null;
        }

        SystemTextJsonSchemaGeneratorSettings settings = new()
        {
            SerializerOptions = _jsonSerializerOptions,
            SchemaNameGenerator = new ComposeSchemaNameGenerator(schemaIdSelector),
            SchemaProcessors =
            {
                new ComposeSchemaProcessor(contentTypeInfo, schemaIdSelector, _jsonSerializerOptions, ComposeNodeUrl),
            },
        };

        bool isElement = contentTypeInfo.IsElement;
        Type baseType = isElement ? typeof(IApiElement) : typeof(IApiContent);
        JsonSchema schema = JsonSchema.FromType(baseType, settings);

        return PostProcessSchema(schema, isElement);
    }

    private static JsonElement PostProcessSchema(JsonSchema schema, bool isElement)
    {
        JsonObject root = JsonNode.Parse(schema.ToJson())!.AsObject();

        _ = root.Remove("title");

        if (root["definitions"] is JsonObject definitions)
        {
            definitions.Remove("IApiElementModel");

            if (definitions.Count == 0)
            {
                root.Remove("definitions");
            }
        }

        ReplaceElementModelRefs(root);

        root["$schema"] = ComposeSchemaUrl;

        JsonArray allOf =
        [
            new JsonObject { ["$ref"] = nameof(IApiElement), },
        ];

        if (!isElement)
        {
            allOf.Add(new JsonObject { ["$ref"] = ComposeNodeUrl, });
            allOf.Add(new JsonObject { ["$ref"] = nameof(IApiContent), });
        }

        root["allOf"] = allOf;

        return JsonDocument.Parse(root.ToJsonString()).RootElement;
    }

    private static void ReplaceElementModelRefs(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            if (obj.TryGetPropertyValue("$ref", out JsonNode? refValue) &&
                refValue?.GetValue<string>() == "#/definitions/IApiElementModel")
            {
                obj["$ref"] = "IApiElementModel";
            }

            foreach (JsonNode? child in obj.Select(kvp => kvp.Value))
            {
                ReplaceElementModelRefs(child);
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (JsonNode? item in arr)
            {
                ReplaceElementModelRefs(item);
            }
        }
    }

    private sealed class ComposeSchemaNameGenerator(ISchemaIdSelector schemaIdSelector) : ISchemaNameGenerator
    {
        public string Generate(Type type)
        {
            return schemaIdSelector.SchemaId(type);
        }
    }
}
