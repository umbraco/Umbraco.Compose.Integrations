using System.Text.Json.Serialization;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

/// <summary>
/// Represents a JSON Schema instance with all supported keywords and constraints.
/// This class provides properties for all standard JSON Schema keywords including type definitions,
/// validation constraints, composition operators, and conditional schemas. Use this class to
/// construct, modify, or deserialize JSON Schemas that validate data structures according to
/// the JSON Schema specification. All properties correspond to JSON Schema keywords and are
/// serialized using camelCase naming per the specification.
/// </summary>
public sealed class JsonSchema
{
    /// <summary>
    /// Gets or sets the JSON Schema version URI. This defines which specification version
    /// the schema conforms to.
    /// </summary>
    [JsonPropertyName("$schema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Schema { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier URI for this schema. The ID can be used as a reference
    /// point for $ref keywords within the same schema or across schemas. IDs should be absolute URIs
    /// when possible to avoid resolution ambiguities.
    /// </summary>
    [JsonPropertyName("$id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets a short identifier that can be used as a reference anchor within the schema.
    /// The $anchor keyword allows referencing local definitions using fragment identifiers like
    /// #anchor-name without requiring a full URI path.
    /// </summary>
    [JsonPropertyName("$anchor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Anchor { get; set; }

    /// <summary>
    /// Gets or sets a reference URI pointing to another schema definition. The value can be a
    /// relative URI, absolute URI, or a fragment identifier referencing an anchor within the
    /// current document. When present, this schema defers validation to the referenced schema.
    /// </summary>
    [JsonPropertyName("$ref")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ref { get; set; }

    /// <summary>
    /// Gets or sets a dictionary of reusable schema definitions. Each key is a definition name
    /// that can be referenced using #/$defs/{name} in $ref keywords. Definitions are typically
    /// used to avoid duplication and organize complex schema structures.
    /// </summary>
    [JsonPropertyName("$defs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonSchema>? Defs { get; set; }

    /// <summary>
    /// Gets or sets the content encoding used for the string value. This indicates the encoding
    /// of the data, such as "base64" or "quoted-printable", and is typically used when the
    /// string contains encoded binary data.
    /// </summary>
    [JsonPropertyName("contentEncoding")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContentEncoding { get; set; }

    /// <summary>
    /// Gets or sets the content media type (MIME type) for the string value. This indicates the
    /// type of data, such as "text/plain" or "application/json", and provides semantic context
    /// for interpreting the string content.
    /// </summary>
    [JsonPropertyName("contentMediaType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContentMediaType { get; set; }

    /// <summary>
    /// Gets or sets a discriminator object for selecting among multiple schemas in oneOf or anyOf
    /// compositions. The discriminator specifies which property name to inspect and optionally
    /// provides a mapping from property values to schema references. This is useful for polymorphic
    /// type hierarchies in OpenAPI and other specifications.
    /// </summary>
    [JsonPropertyName("discriminator")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonSchemaDiscriminator? Discriminator { get; set; }

    /// <summary>
    /// Gets or sets a human-readable description of the schema purpose and intent. Descriptions
    /// should be clear and explanatory, helping users understand what data the schema validates
    /// and any specific constraints or requirements.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a title for the schema. Titles are typically short, descriptive names that
    /// provide a human-friendly identifier for the schema, often used in user interfaces or
    /// documentation generation.
    /// </summary>
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the expected data type for values validated against this schema. Valid types
    /// include: "string", "number", "integer", "boolean", "object", "array", and "null". When
    /// not specified, all types are permitted unless constrained by other keywords.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonValueType? Type { get; set; }

    /// <summary>
    /// Gets or sets a format hint for type validation. Formats provide additional semantic
    /// constraints beyond the basic type, such as "date-time", "email", "uri", "uuid", etc.
    /// Format validation is optional and schema validators may choose to ignore unknown formats.
    /// </summary>
    [JsonPropertyName("format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Format { get; set; }

    /// <summary>
    /// Gets or sets an array of allowed literal values. The validated value must exactly match
    /// one of the values in this array using JSON equality semantics. This provides an enumeration
    /// constraint for string, number, boolean, and null values.
    /// </summary>
    [JsonPropertyName("enum")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<object?>? Enum { get; set; }

    /// <summary>
    /// Gets or sets a single literal value that the validated data must exactly match. This is
    /// similar to enum but restricts values to exactly one specific value. The comparison uses JSON
    /// equality semantics and is more efficient than using enum with a single value.
    /// </summary>
    [JsonPropertyName("const")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Const { get; set; }

    /// <summary>
    /// Gets or sets a positive numeric value that the validated number must be divisible by.
    /// The value is considered valid if dividing it by MultipleOf results in an integer (with
    /// no remainder). This constraint applies to both number and integer types.
    /// </summary>
    [JsonPropertyName("multipleOf")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? MultipleOf { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed numeric value. The validated value must be less than or
    /// equal to Maximum unless ExclusiveMaximum is true, in which case it must be strictly less
    /// than this value. This constraint applies to both number and integer types.
    /// </summary>
    [JsonPropertyName("maximum")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Maximum { get; set; }

    /// <summary>
    /// Gets or sets whether the Maximum constraint is exclusive. When true, the validated value
    /// must be strictly less than ExclusiveMaximum; when false (default), values equal to the
    /// maximum are considered valid. This provides an upper bound that excludes or includes equality.
    /// </summary>
    [JsonPropertyName("exclusiveMaximum")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? ExclusiveMaximum { get; set; }

    /// <summary>
    /// Gets or sets the minimum allowed numeric value. The validated value must be greater than
    /// or equal to Minimum unless ExclusiveMinimum is true, in which case it must be strictly
    /// greater than this value. This constraint applies to both number and integer types.
    /// </summary>
    [JsonPropertyName("minimum")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Minimum { get; set; }

    /// <summary>
    /// Gets or sets whether the Minimum constraint is exclusive. When true, the validated value
    /// must be strictly greater than ExclusiveMinimum; when false (default), values equal to the
    /// minimum are considered valid. This provides a lower bound that excludes or includes equality.
    /// </summary>
    [JsonPropertyName("exclusiveMinimum")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? ExclusiveMinimum { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed string length in characters. The validated string must
    /// contain no more characters than this value. For arrays, use MaxItems instead. This constraint
    /// applies only to string types.
    /// </summary>
    [JsonPropertyName("maxLength")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the minimum allowed string length in characters. The validated string must
    /// contain at least this many characters. For arrays, use MinItems instead. This constraint
    /// applies only to string types.
    /// </summary>
    [JsonPropertyName("minLength")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MinLength { get; set; }

    /// <summary>
    /// Gets or sets a regular expression pattern for validating strings. The validated string
    /// must match this pattern according to ECMA-262 regex semantics. Patterns should be tested
    /// thoroughly before use as invalid patterns will cause validation errors.
    /// </summary>
    [JsonPropertyName("pattern")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed array elements. The validated array must contain no more
    /// items than this value. This constraint applies only to array types and works in conjunction
    /// with MinItems to define array size boundaries.
    /// </summary>
    [JsonPropertyName("maxItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxItems { get; set; }

    /// <summary>
    /// Gets or sets the minimum allowed array elements. The validated array must contain at least
    /// this many items. This constraint applies only to array types and works in conjunction with
    /// MaxItems to define array size boundaries.
    /// </summary>
    [JsonPropertyName("minItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MinItems { get; set; }

    /// <summary>
    /// Gets or sets whether all array items must be unique. When true, no two items in the array
    /// can be equal according to JSON equality semantics. This is useful for validating collections
    /// where duplicates are not allowed, such as unique identifiers.
    /// </summary>
    [JsonPropertyName("uniqueItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? UniqueItems { get; set; }

    /// <summary>
    /// Gets or sets a subschema that validates array elements. At least one item in the validated
    /// array must match this schema. This is different from Items, which applies to all items.
    /// Contains provides existential validation rather than universal validation of array contents.
    /// </summary>
    [JsonPropertyName("contains")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonSchema? Contains { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed object properties. The validated object must contain no
    /// more property keys than this value. This constraint applies only to object types and works
    /// in conjunction with MinProperties to define object size boundaries.
    /// </summary>
    [JsonPropertyName("maxProperties")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxProperties { get; set; }

    /// <summary>
    /// Gets or sets the minimum allowed object properties. The validated object must contain at
    /// least this many property keys. This constraint applies only to object types and works in
    /// conjunction with MaxProperties to define object size boundaries.
    /// </summary>
    [JsonPropertyName("minProperties")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MinProperties { get; set; }

    /// <summary>
    /// Gets or sets an array of required property names for objects. The validated object must
    /// contain all properties listed in this array. Required properties must be present regardless
    /// of whether they have null values, unless explicitly allowed by the property's type schema.
    /// </summary>
    [JsonPropertyName("required")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Required { get; set; }

    /// <summary>
    /// Gets or sets a dictionary mapping property names to their subschemas. Each key is a property
    /// name and each value is a schema that validates that specific property. Properties not listed
    /// in this dictionary are validated against AdditionalProperties if specified, otherwise allowed.
    /// </summary>
    [JsonPropertyName("properties")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonSchema>? Properties { get; set; }

    /// <summary>
    /// Gets or sets a dictionary mapping regex patterns to subschemas. Object properties whose
    /// names match the pattern are validated against the corresponding schema. This allows wildcard
    /// property validation for dynamic or structured property names like email addresses or IDs.
    /// </summary>
    [JsonPropertyName("patternProperties")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonSchema>? PatternProperties { get; set; }

    /// <summary>
    /// Gets or sets a schema for validating additional properties not defined in the Properties
    /// dictionary. When set to null (default), additional properties are allowed without validation.
    /// When set to false, additional properties are forbidden. When set to a schema, they must
    /// match that schema. This controls permissive vs restrictive object validation behavior.
    /// </summary>
    [JsonPropertyName("additionalProperties")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonSchema? AdditionalProperties { get; set; }

    /// <summary>
    /// Gets or sets a subschema for validating array items. When Items is a schema, all array
    /// elements must match this schema. For tuple validation with different types per position,
    /// use PrefixItems and AdditionalItems instead. This provides uniform item validation.
    /// </summary>
    [JsonPropertyName("items")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonSchema? Items { get; set; }

    /// <summary>
    /// Gets or sets an array of subschemas for tuple validation. Each position in the validated
    /// array is validated against the corresponding schema in this array. Positions beyond the
    /// array length are validated against AdditionalItems if specified, otherwise allowed. This
    /// enables strict positional type checking for fixed-length arrays.
    /// </summary>
    [JsonPropertyName("prefixItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<JsonSchema>? PrefixItems { get; set; }

    /// <summary>
    /// Gets or sets an array of subschemas where the validated value must match all of them.
    /// AllOf provides intersection validation, requiring a value to satisfy every schema in the
    /// array. This is commonly used for composing complex validation rules from simpler ones.
    /// </summary>
    [JsonPropertyName("allOf")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<JsonSchema>? AllOf { get; set; }

    /// <summary>
    /// Gets or sets an array of subschemas where the validated value must match at least one of
    /// them. AnyOf provides union validation, allowing a value that satisfies any schema in the
    /// array. This is useful for accepting multiple valid formats or types for the same semantic value.
    /// </summary>
    [JsonPropertyName("anyOf")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<JsonSchema>? AnyOf { get; set; }

    /// <summary>
    /// Gets or sets an array of subschemas where the validated value must match exactly one of
    /// them. OneOf provides exclusive union validation, requiring a value to satisfy precisely
    /// one schema in the array. This is stricter than AnyOf and useful for discriminated unions.
    /// </summary>
    [JsonPropertyName("oneOf")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<JsonSchema>? OneOf { get; set; }

    /// <summary>
    /// Gets or sets a subschema that the validated value must not match. Not provides negation
    /// validation, rejecting values that satisfy this schema while accepting all others. This is
    /// useful for excluding specific values or patterns from an otherwise permissive schema.
    /// </summary>
    [JsonPropertyName("not")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonSchema? Not { get; set; }

    /// <summary>
    /// Gets or sets a subschema evaluated for conditional validation. When If is present, the
    /// Then and/or Else schemas are conditionally applied based on whether the validated value
    /// matches the If schema. This enables dynamic validation rules based on data content.
    /// </summary>
    [JsonPropertyName("if")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonSchema? If { get; set; }

    /// <summary>
    /// Gets or sets a subschema applied when the If condition is satisfied. When the validated
    /// value matches the If schema, it must also match the Then schema. This enables positive
    /// conditional validation rules that apply only under specific conditions.
    /// </summary>
    [JsonPropertyName("then")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonSchema? Then { get; set; }

    /// <summary>
    /// Gets or sets a subschema applied when the If condition is not satisfied. When the validated
    /// value does not match the If schema, it must match the Else schema (if present). This enables
    /// fallback validation rules for alternative data states or conditions.
    /// </summary>
    [JsonPropertyName("else")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonSchema? Else { get; set; }

    /// <summary>
    /// Gets or sets a dictionary of extension keywords not defined in the JSON Schema specification.
    /// Extensions allow adding custom validation logic or metadata while maintaining compatibility
    /// with standard JSON Schema validators that ignore unknown keywords. Use with caution.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?>? Extensions { get; set; }

    /// <summary>
    /// Creates a new JsonSchema instance.
    /// </summary>
    /// <returns>A new JsonSchema instance</returns>
    public static JsonSchema Create() =>
        new();

    /// <summary>
    /// Adds a custom extension keyword to the schema's Extensions dictionary. This allows adding
    /// non-standard keywords for vendor-specific validation or metadata while maintaining the
    /// schema structure. If Extensions is null, it is initialized before adding the key-value pair.
    /// </summary>
    /// <param name="key">The extension keyword name to add. Must be a valid JSON property name.</param>
    /// <param name="value">The value for the extension keyword. Can be any serializable JSON value including null.</param>
    /// <returns>This JsonSchema instance to enable method chaining for fluent schema construction</returns>
    public JsonSchema AddExtension(string key, object? value)
    {
        Extensions ??= [];
        Extensions[key] = value;
        return this;
    }
}
