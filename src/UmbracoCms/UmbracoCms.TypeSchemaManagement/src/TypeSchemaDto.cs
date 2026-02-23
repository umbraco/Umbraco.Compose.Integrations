using System.Text.Json.Serialization;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal record TypeSchemaDto(
    [property: JsonPropertyName("typeSchemaAlias")]
    string TypeSchemaAlias,
    [property: JsonPropertyName("description")]
    string? Description,
    [property: JsonPropertyName("schema")] JsonSchema Schema
);
