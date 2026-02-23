using System.Text.Json.Serialization;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

/// <summary>
/// Represents a JSON Schema discriminator object used for selecting among multiple schemas in
/// oneOf or anyOf compositions. Discriminators are commonly used in OpenAPI and other specifications
/// to enable polymorphic type validation by inspecting a specific property's value to determine
/// which schema variant should be applied. This class contains the property name to inspect and
/// an optional mapping from property values to schema references.
/// </summary>
public sealed class JsonSchemaDiscriminator
{
    /// <summary>
    /// Gets or sets the name of the property used as a discriminator for selecting among
    /// polymorphic types. This property's value determines which schema variant should be
    /// applied for validation in oneOf or anyOf compositions.
    /// </summary>
    [JsonPropertyName("propertyName")]
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets or sets an optional mapping from discriminator property values to schema references.
    /// Each key is a possible value for the propertyName, and each value is a URI fragment
    /// pointing to the corresponding schema definition. This enables automatic schema selection
    /// based on discriminator values without manual condition checking.
    /// </summary>
    [JsonPropertyName("mapping")]
    public Dictionary<string, string>? Mapping { get; set; }
}
