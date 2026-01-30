using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Compose.Integrations.UmbracoCms.Schema;

internal record CreateTypeSchemaRequest(
    [property: JsonPropertyName("typeSchemaAlias")]
    string TypeSchemaAlias,
    [property: JsonPropertyName("description")]
    string? Description,
    [property: JsonPropertyName("schema")] JsonElement Schema
);
