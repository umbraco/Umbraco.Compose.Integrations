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
public class JsonSchemaGeneratorOptions
{
    /// <summary>
    /// Gets or sets the reference mode for schema generation, controlling how type references
    /// are expressed in the output. Defs mode uses $defs with fragment references, Inline mode
    /// embeds all schemas directly without references, and External mode generates separate
    /// schemas with URI-based references. Default is ReferenceMode.Defs for self-contained schemas.
    /// </summary>
    public ReferenceMode ReferenceMode { get; set; } = ReferenceMode.Defs;

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
    /// Gets the dictionary mapping primitive/special types to their schema builder actions.
    /// Each type is associated with an action that configures the JsonSchemaBuilder for that type,
    /// including setting the JSON Schema type and any format. This allows customization of how
    /// primitive types (like DateTime, Guid, etc.) are handled during schema generation.
    /// Default handlers are pre-populated for common types.
    /// </summary>
    public Dictionary<Type, Action<JsonSchemaBuilder>> PrimitiveTypeHandlers { get; } = new()
    {
        { typeof(string), builder => builder.Type(JsonValueType.String) },
        { typeof(char), builder => builder.Type(JsonValueType.String).Format("char") },
        { typeof(byte), builder => builder.Type(JsonValueType.Integer).Format("uint8") },
        { typeof(sbyte), builder => builder.Type(JsonValueType.Integer).Format("int8") },
        { typeof(short), builder => builder.Type(JsonValueType.Integer).Format("int16") },
        { typeof(ushort), builder => builder.Type(JsonValueType.Integer).Format("uint16") },
        { typeof(int), builder => builder.Type(JsonValueType.Integer).Format("int32") },
        { typeof(uint), builder => builder.Type(JsonValueType.Integer).Format("uint32") },
        { typeof(long), builder => builder.Type(JsonValueType.Integer).Format("int64") },
        { typeof(ulong), builder => builder.Type(JsonValueType.Integer).Format("uint64") },
        { typeof(float), builder => builder.Type(JsonValueType.Number).Format("float") },
        { typeof(double), builder => builder.Type(JsonValueType.Number).Format("double") },
        { typeof(decimal), builder => builder.Type(JsonValueType.Number).Format("decimal") },
        { typeof(BigInteger), builder => builder.Type(JsonValueType.Number).Format("decimal128") },
        { typeof(bool), builder => builder.Type(JsonValueType.Boolean) },
        { typeof(Guid), builder => builder.Type(JsonValueType.String).Format("uuid") },
        { typeof(Uri), builder => builder.Type(JsonValueType.String).Format("uri") },
        { typeof(DateTime), builder => builder.Type(JsonValueType.String).Format("date-time") },
        { typeof(DateTimeOffset), builder => builder.Type(JsonValueType.String).Format("date-time") },
        { typeof(DateOnly), builder => builder.Type(JsonValueType.String).Format("date") },
        { typeof(TimeOnly), builder => builder.Type(JsonValueType.String).Format("time") },
        { typeof(TimeSpan), builder => builder.Type(JsonValueType.String).Format("duration") },
        { typeof(Regex), builder => builder.Type(JsonValueType.String).Format("regex") },
        { typeof(Version), builder => builder.Type(JsonValueType.String) },
        { typeof(System.Net.IPAddress), builder => builder.Type(JsonValueType.String) }
    };
}
