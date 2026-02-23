using System.Text.Json;
using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.DataSource;

/// <summary>
/// Represents the configuration for the Umbraco Compose Content Picker Data Source.
/// Values defined from the umbraco-compopse-integrations-cms-data-source bundle-manifests.ts are realized here, and are used to query content via GraphQL.
/// </summary>
public sealed record UmbracoComposeContentPickerDataSourceConfiguration
{
    internal string? SearchField { get; }
    internal string Collection { get; } = string.Empty;
    internal string TypeSchema { get; } = string.Empty;
    internal string? Variant { get; set; } = string.Empty;
    internal IReadOnlyCollection<string> IncludeFields { get; } = [];

    private UmbracoComposeContentPickerDataSourceConfiguration()
    {
    }

    internal UmbracoComposeContentPickerDataSourceConfiguration(IDataType dataType)
    {
        if (dataType?.ConfigurationData is null)
        {
            return;
        }

        Variant = dataType.ConfigurationData.GetValue(ConfigurationKeys.Variant) as string;
        object? includeFields = dataType.ConfigurationData.GetValue(ConfigurationKeys.TypeSchemaIncludeFields);
        object? searchField = dataType.ConfigurationData.GetValue(ConfigurationKeys.SearchField);

        SearchField = ExtractFieldNameFrom(searchField);

        if (dataType.ConfigurationData.GetValue(ConfigurationKeys.Collection) is not string collection)
        {
            throw new InvalidOperationException($"The data type configuration for '{ConfigurationKeys.Collection}' is not valid.");
        }

        if (dataType.ConfigurationData.GetValue(ConfigurationKeys.TypeSchema) is not string typeSchema)
        {
            throw new InvalidOperationException($"The data type configuration for '{ConfigurationKeys.TypeSchema}' is not valid.");
        }

        if (includeFields is not JsonObject includeFieldsJsonObject)
        {
            throw new InvalidOperationException(
                $"The data type configuration for '{ConfigurationKeys.TypeSchemaIncludeFields}' is not valid.");
        }

        _ = includeFieldsJsonObject.TryGetPropertyValue(ConfigurationKeys.TypeSchemaFieldsProperty, out JsonNode? node);
        IncludeFields = TypeSchemaFieldFromJsonObject(node as JsonArray);

        if (IncludeFields.Count == 0)
        {
            throw new InvalidOperationException(
                $"The data type configuration for '{ConfigurationKeys.TypeSchemaIncludeFields}' is not valid.");
        }

        Collection = collection.ToGraphQLFieldNameCase();
        TypeSchema = typeSchema;
    }

    private static string? ExtractFieldNameFrom(object? searchField)
    {
        if (searchField is not JsonObject o)
        {
            return null;
        }

        bool hasFields = o.TryGetPropertyValue("fields", out JsonNode? fields);

        return !hasFields || fields is not JsonArray a || a.Count == 0 ? null : (a[0]?.GetValue<string>());
    }

    private static string[] TypeSchemaFieldFromJsonObject(JsonArray? json)
    {
        if (json is null || json.Count == 0) { return []; }

        List<string> fields = [];

        foreach (JsonNode? node in json)
        {
            TypeSchemaIncludedField? field = node.Deserialize<TypeSchemaIncludedField>();
            if (!string.IsNullOrEmpty(field?.typeSchemaField))
            {
                fields.Add(field.typeSchemaField);
            }
        }

        return [.. fields];
    }

    private static class ConfigurationKeys
    {
        public const string Variant = "composeVariant";
        public const string Collection = "composeCollection";
        public const string TypeSchema = "composeTypeSchema";
        public const string TypeSchemaIncludeFields = "composeTypeSchemaIncludeFields";
        public const string TypeSchemaFieldsProperty = "typeSchemaFields";
        public const string SearchField = "composeSearchFields";
    }

    private sealed class TypeSchemaIncludedField
    {
        public string typeSchemaField { get; set; } = string.Empty;
    }
}
