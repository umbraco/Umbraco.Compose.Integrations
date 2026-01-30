using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NJsonSchema;
using NJsonSchema.Generation;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.DeliveryApi;

namespace Umbraco.Compose.Integrations.UmbracoCms.Schema;

internal class JsonSchemaExporterService(
    IContentTypeSchemaService contentTypeSchemaService,
    ISchemaIdSelector schemaIdSelector,
    IOptionsMonitor<JsonOptions> options)
{
    private const string ComposeSchemaUrl = "https://umbracocompose.com/v1/schema";
    private const string ComposeNodeUrl = "https://umbracocompose.com/v1/node";

    private readonly JsonSerializerOptions _jsonSerializerOptions =
        options.Get(Constants.JsonOptionsNames.DeliveryApi).JsonSerializerOptions;

    public JsonElement GenerateSchema(string contentTypeAlias)
    {
        var contentTypeInfo = contentTypeSchemaService.GetDocumentTypeByAlias(contentTypeAlias);

        var settings = new SystemTextJsonSchemaGeneratorSettings
        {
            SerializerOptions = _jsonSerializerOptions,
            SchemaNameGenerator = new ComposeSchemaNameGenerator(schemaIdSelector),
            SchemaProcessors =
            {
                new ComposeSchemaProcessor(contentTypeInfo, schemaIdSelector, _jsonSerializerOptions, ComposeNodeUrl)
            }
        };

        var isElement = contentTypeInfo.IsElement;
        var baseType = isElement ? typeof(IApiElement) : typeof(IApiContent);
        var schema = JsonSchema.FromType(baseType, settings);

        return PostProcessSchema(schema, isElement);
    }

    private static JsonElement PostProcessSchema(JsonSchema schema, bool isElement)
    {
        var json = schema.ToJson();
        var root = JsonNode.Parse(json)!.AsObject();

        root.Remove("title");

        root["$schema"] = ComposeSchemaUrl;

        var allOf = new JsonArray
        {
            new JsonObject { ["$ref"] = ComposeNodeUrl },
            new JsonObject { ["$ref"] = nameof(IApiElement) }
        };

        if (!isElement)
        {
            allOf.Add(new JsonObject { ["$ref"] = nameof(IApiContent) });
        }

        root["allOf"] = allOf;

        return JsonDocument.Parse(root.ToJsonString()).RootElement;
    }

    private class ComposeSchemaNameGenerator(ISchemaIdSelector schemaIdSelector) : ISchemaNameGenerator
    {
        public string Generate(Type type) => schemaIdSelector.SchemaId(type);
    }
}