using System.Text.Json.Serialization;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

/// <summary>
/// Represents the JSON Schema type categories as defined in the JSON Schema specification.
/// Each type corresponds to a fundamental data category that values can be validated against.
/// These types form the basis for all schema validation and are mapped to their JSON string
/// equivalents when serializing schemas.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<JsonValueType>))]
public enum JsonValueType
{
    /// <summary>
    /// Represents string values - sequences of Unicode characters. String validation includes
    /// constraints like maxLength, minLength, pattern matching, and format restrictions such
    /// as email, uri, or date-time formats.
    /// </summary>
    [JsonStringEnumMemberName("string")]
    String = 0,

    /// <summary>
    /// Represents numeric values with fractional parts (floating-point numbers). Number type
    /// allows both integers and decimals and supports constraints like maximum, minimum,
    /// multipleOf, exclusiveMaximum, and exclusiveMinimum.
    /// </summary>
    [JsonStringEnumMemberName("number")]
    Number = 1,

    /// <summary>
    /// Represents integer values - whole numbers without fractional parts. Integer type is
    /// similar to Number but semantically indicates the value should be a whole number.
    /// All integer constraints apply equally to this type.
    /// </summary>
    [JsonStringEnumMemberName("integer")]
    Integer = 2,

    /// <summary>
    /// Represents boolean values - true or false logical states. Boolean validation simply
    /// checks that the value is either true or false with no additional constraints possible.
    /// </summary>
    [JsonStringEnumMemberName("boolean")]
    Boolean = 3,

    /// <summary>
    /// Represents object values - unordered collections of property name/value pairs. Object
    /// validation includes property constraints like required, properties, patternProperties,
    /// additionalProperties, maxProperties, and minProperties.
    /// </summary>
    [JsonStringEnumMemberName("object")]
    Object = 4,

    /// <summary>
    /// Represents array values - ordered sequences of elements. Array validation includes
    /// item constraints via items or prefixItems, size constraints via minItems/maxItems,
    /// uniqueness via uniqueItems, and containment via contains.
    /// </summary>
    [JsonStringEnumMemberName("array")]
    Array = 5,

    /// <summary>
    /// Represents null values - the absence of any value. Null type validation simply checks
    /// that the value is null. This type is useful for schemas that accept null as a valid
    /// option alongside other types.
    /// </summary>
    [JsonStringEnumMemberName("null")]
    Null = 6
}
