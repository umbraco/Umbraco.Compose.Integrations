using System.Net;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

/// <summary>
/// Configuration options for JsonSchemaGenerator that control reference handling, custom type
/// handler registration, and property name transformation. These options determine how generated
/// schemas handle type references, whether to inline or externalize definitions, which custom
/// handlers participate in schema generation, and how .NET property names are converted to JSON
/// property names. Modify these options before calling Generate methods.
/// </summary>
public sealed class JsonSchemaGeneratorOptions
{
    /// <summary>
    /// Gets the default JsonSchemaGeneratorOptions instance with standard settings including Inline
    /// reference mode, no property naming policy, empty handler list, and default type name generator.
    /// This static property provides a ready-to-use configuration for typical schema generation scenarios
    /// where automatic reference inlining and default naming conventions are acceptable.
    /// </summary>
    public static JsonSchemaGeneratorOptions Default { get; } = new();

    /// <summary>
    /// Gets or sets the reference mode for schema generation, controlling how type references
    /// are expressed in the output. Defs mode uses $defs with fragment references, Inline mode
    /// embeds all schemas directly without references, and External mode generates separate
    /// schemas with URI-based references. Default is ReferenceMode.Inline.
    /// </summary>
    public ReferenceMode ReferenceMode { get; set; } = ReferenceMode.Inline;

    /// <summary>
    /// Gets or sets the naming policy for transforming .NET property names to JSON property names.
    /// This controls how property names like CamelCaseProperty are converted (e.g., to camelCaseProperty).
    /// When null, property names are used as-is without transformation. Common values include
    /// JsonNamingPolicy.CamelCase for standard JSON conventions or custom policies for specific needs.
    /// </summary>
    public JsonNamingPolicy? PropertyNamingPolicy { get; set; }

    /// <summary>
    /// Gets the list of custom type handlers registered for extending schema generation. Handlers
    /// are checked in registration order when generating schemas for types, and the first handler
    /// that reports CanHandle returns true is used to generate the schema. This list is initialized
    /// empty and can be populated with custom implementations before calling Generate methods.
    /// </summary>
    public List<IJsonSchemaTypeHandler> Handlers { get; } = [];

    /// <summary>
    /// Gets or sets the generator used for creating type names in schema definitions. Controls how
    /// types are named in $defs and other schema references, allowing customization of naming
    /// conventions for generated schema components.
    /// </summary>
    public TypeNameGenerator TypeNameGenerator { get; set; } = TypeNameGenerator.Default;

    /// <summary>
    /// Gets or sets the default $schema URI to use for object schemas when creating builders. This
    /// URI specifies which JSON Schema draft version the generated objects conform to, typically
    /// "https://json-schema.org/draft/2020-12/schema". When null, object builders will not set a
    /// $schema declaration unless explicitly configured via the Schema method.
    /// </summary>
    public string? DefaultSchema { get; set; }

    /// <summary>
    /// Gets the dictionary mapping .NET types to their schema builder actions. Each type is associated
    /// with an action that configures the JsonSchemaBuilder for that type, including setting the JSON
    /// Schema type and any format. This allows customization of how types (like DateTime, Guid, etc.)
    /// are handled during schema generation. Default handlers are pre-populated for common primitive
    /// types and framework types to ensure consistent JSON Schema representation across generations.
    /// </summary>
    public Dictionary<Type, Action<JsonSchemaGeneratorContext, JsonSchemaBuilder>> TypeMapping { get; } = new()
    {
        { typeof(string), (_, builder) => builder.Type(JsonPropertyType.String) },
        { typeof(char), (_, builder) => builder.Type(JsonPropertyType.String).Format("char") },
        { typeof(byte), (_, builder) => builder.Type(JsonPropertyType.Integer).Format("uint8") },
        { typeof(sbyte), (_, builder) => builder.Type(JsonPropertyType.Integer).Format("int8") },
        { typeof(short), (_, builder) => builder.Type(JsonPropertyType.Integer).Format("int16") },
        { typeof(ushort), (_, builder) => builder.Type(JsonPropertyType.Integer).Format("uint16") },
        { typeof(int), (_, builder) => builder.Type(JsonPropertyType.Integer).Format("int32") },
        { typeof(uint), (_, builder) => builder.Type(JsonPropertyType.Integer).Format("uint32") },
        { typeof(long), (_, builder) => builder.Type(JsonPropertyType.Integer).Format("int64") },
        { typeof(ulong), (_, builder) => builder.Type(JsonPropertyType.Integer).Format("uint64") },
        { typeof(float), (_, builder) => builder.Type(JsonPropertyType.Number).Format("float") },
        { typeof(double), (_, builder) => builder.Type(JsonPropertyType.Number).Format("double") },
        { typeof(decimal), (_, builder) => builder.Type(JsonPropertyType.Number).Format("decimal") },
        { typeof(BigInteger), (_, builder) => builder.Type(JsonPropertyType.Number).Format("decimal128") },
        { typeof(bool), (_, builder) => builder.Type(JsonPropertyType.Boolean) },
        { typeof(Guid), (_, builder) => builder.Type(JsonPropertyType.String).Format("uuid") },
        { typeof(Uri), (_, builder) => builder.Type(JsonPropertyType.String).Format("uri") },
        { typeof(DateTime), (_, builder) => builder.Type(JsonPropertyType.String).Format("date-time") },
        { typeof(DateTimeOffset), (_, builder) => builder.Type(JsonPropertyType.String).Format("date-time") },
        { typeof(DateOnly), (_, builder) => builder.Type(JsonPropertyType.String).Format("date") },
        { typeof(TimeOnly), (_, builder) => builder.Type(JsonPropertyType.String).Format("time") },
        { typeof(TimeSpan), (_, builder) => builder.Type(JsonPropertyType.String).Format("duration") },
        { typeof(Regex), (_, builder) => builder.Type(JsonPropertyType.String).Format("regex") },
        { typeof(Version), (_, builder) => builder.Type(JsonPropertyType.String) },
        { typeof(IPAddress), (_, builder) => builder.Type(JsonPropertyType.String) }
    };
}
