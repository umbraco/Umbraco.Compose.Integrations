using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Namotion.Reflection;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.References;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.DeliveryApi;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal class ComposeSchemaProcessor(
    ContentTypeSchemaInfo contentTypeInfo,
    ISchemaIdSelector schemaIdSelector,
    JsonSerializerOptions jsonSerializerOptions,
    string composeNodeUrl)
    : ISchemaProcessor
{
    private readonly HashSet<Type> _handledTypes = [];

    public void Process(SchemaProcessorContext context)
    {
        if (context.Resolver.RootObject != context.Schema)
        {
            return;
        }

        if (!contentTypeInfo.IsElement)
        {
            AddInterfaceProperties(typeof(IApiElement), context);
        }

        AddContentTypeProperties(context);
    }

    private void AddInterfaceProperties(Type interfaceType, SchemaProcessorContext context)
    {
        JsonTypeInfo jsonTypeInfo = GetJsonTypeInfo(interfaceType);

        foreach (JsonPropertyInfo jsonProperty in jsonTypeInfo.Properties)
        {
            context.Schema.Properties[jsonProperty.Name] = context.Generator.GenerateWithReference<JsonSchemaProperty>(
                jsonProperty.PropertyType.ToContextualType(), context.Resolver);
        }
    }

    private void AddContentTypeProperties(SchemaProcessorContext context)
    {
        JsonSchemaProperty propertiesSchema = new()
        {
            Type = JsonObjectType.Object
        };

        foreach (ContentTypePropertySchemaInfo property in contentTypeInfo.Properties)
        {
            propertiesSchema.Properties[property.Alias] = GetOrCreateSchema(property.DeliveryApiClrType, context);
        }

        context.Schema.Properties["properties"] = propertiesSchema;
    }

    private JsonSchemaProperty GetOrCreateSchema(Type type, SchemaProcessorContext context)
    {
        if (ComposeNodeTypeHelper.IsComposeNodeType(type))
        {
            return CreatePropertyWithReference(composeNodeUrl);
        }

        JsonTypeInfo jsonTypeInfo = GetJsonTypeInfo(type);
        switch (jsonTypeInfo.Kind)
        {
            case JsonTypeInfoKind.Enumerable:
                Type elementType = jsonTypeInfo.ElementType ?? typeof(object);
                JsonSchemaProperty itemSchema = GetOrCreateSchema(elementType, context);

                return new()
                {
                    Type = JsonObjectType.Array,
                    Item = itemSchema,
                };

            case JsonTypeInfoKind.Object when !_handledTypes.Add(type):
                string schemaId = schemaIdSelector.SchemaId(type);
                return CreatePropertyWithReference($"#/definitions/{schemaId}");

            default:
                return context.Generator.GenerateWithReference<JsonSchemaProperty>(
                    type.ToContextualType(), context.Resolver);
        }
    }

    private static JsonSchemaProperty CreatePropertyWithReference(string path)
    {
        JsonSchemaProperty reference = new();
        ((IJsonReferenceBase)reference).ReferencePath = path;
        return reference;
    }

    private JsonTypeInfo GetJsonTypeInfo(Type type)
    {
        JsonTypeInfo? jsonTypeInfo = jsonSerializerOptions.TypeInfoResolver?.GetTypeInfo(type, jsonSerializerOptions);
        return jsonTypeInfo ??
            throw new InvalidOperationException($"Could not get JsonTypeInfo for type {type.FullName}");
    }
}