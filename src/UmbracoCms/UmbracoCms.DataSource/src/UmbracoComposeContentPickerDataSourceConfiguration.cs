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
    internal string KeyField { get; } = string.Empty;
    internal string? SearchField { get; }
    internal string Collection { get; } = string.Empty;
    internal string TypeSchema { get; } = string.Empty;
    internal string? Variant { get; set; } = string.Empty;
    internal IEnumerable<string> IncludeFields { get; } = [];

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
        var collection = (dataType.ConfigurationData.GetValue(ConfigurationKeys.Collection) as string);
        var typeSchema = (dataType.ConfigurationData.GetValue(ConfigurationKeys.TypeSchema) as string);
        object? includeFields = dataType.ConfigurationData.GetValue(ConfigurationKeys.TypeSchemaIncludeFields);
        object? searchField = dataType.ConfigurationData.GetValue(ConfigurationKeys.SearchField);
        object? keyField = dataType.ConfigurationData.GetValue(ConfigurationKeys.KeyField);

        SearchField = ExtractFieldNameFrom(searchField);
        KeyField = ExtractFieldNameFrom(keyField)
            ?? throw new InvalidOperationException($"The data type configuration for '{ConfigurationKeys.KeyField}' is not valid.");

        if (collection is null)
        {
            throw new InvalidOperationException($"The data type configuration for '{ConfigurationKeys.Collection}' is not valid.");
        }

        if (typeSchema is null)
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

        if (!IncludeFields.Any())
        {
            throw new InvalidOperationException(
                $"The data type configuration for '{ConfigurationKeys.TypeSchemaIncludeFields}' is not valid.");
        }

        Collection = collection;
        TypeSchema = typeSchema;
    }

    private static string? ExtractFieldNameFrom(object? searchField)
    {
        if (searchField is not JsonObject o)
        {
            return null;
        }

        bool hasFields = o.TryGetPropertyValue("fields", out JsonNode? fields);

        if (!hasFields || fields is not JsonArray a || a.Count == 0)
        {
            return null;
        }

        return a[0]?.GetValue<string>();
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

        return [.. fields,];
    }

    private static class ConfigurationKeys
    {
        public const string Variant = "composeVariant";
        public const string Collection = "composeCollection";
        public const string TypeSchema = "composeTypeSchema";
        public const string TypeSchemaIncludeFields = "composeTypeSchemaIncludeFields";
        public const string TypeSchemaFieldsProperty = "typeSchemaFields";
        public const string SearchField = "composeSearchFields";
        public const string KeyField = "composeKeyField";
    }

    private sealed class TypeSchemaIncludedField
    {
        public string typeSchemaField { get; set; } = string.Empty;
    }
}
