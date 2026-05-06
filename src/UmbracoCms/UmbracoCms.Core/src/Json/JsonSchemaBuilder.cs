namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

/// <summary>
/// Provides a fluent builder API for constructing JSON Schemas.
/// The builder pattern enables intuitive schema creation through method chaining, where each method
/// configures a specific aspect of the schema and returns the builder for further configuration.
/// </summary>
public sealed class JsonSchemaBuilder
{
    /// <summary>
    /// Gets the JsonSchema instance being built by this builder. This property exposes the schema
    /// object that accumulates all configuration applied through the builder's methods. The schema
    /// is initialized as empty and progressively populated as builder methods are called. Use this
    /// property to access the final schema after calling Build() or to inspect intermediate state
    /// during fluent configuration.
    /// </summary>
    public JsonSchema JsonSchema { get; }

    /// <summary>
    /// Initializes a new SchemaBuilder instance with a default schema configured with the
    /// JSON Schema Draft 2020-12 URI. This constructor is called by the parameterless Create()
    /// factory method and provides a properly initialized starting point for schema construction.
    /// </summary>
    public JsonSchemaBuilder() : this(JsonSchema.Create()) { }

    /// <summary>
    /// Creates a new SchemaBuilder instance initialized with a default schema containing the
    /// JSON Schema Draft 2020-12 URI. This is the recommended entry point for building complete
    /// schemas that will be serialized and used independently. The returned builder has all
    /// other properties null and ready for configuration via method chaining.
    /// </summary>
    /// <returns>A new SchemaBuilder instance configured with the 2020-12 draft schema URI</returns>
    public static JsonSchemaBuilder Create() =>
        new();

    /// <summary>
    /// Sets the JSON Schema draft version URI for this schema. This defines which specification
    /// version the schema conforms to and should typically be set to "https://json-schema.org/draft/2020-12/schema".
    /// This method is primarily useful when creating schemas that need to declare a specific
    /// draft version different from the default 2020-12.
    /// </summary>
    /// <param name="schemaUri">The URI identifying the JSON Schema draft version. Set to null to remove the $schema declaration.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Schema(string? schemaUri)
    {
        JsonSchema.Schema = schemaUri;
        return this;
    }

    private JsonSchemaBuilder(JsonSchema schema)
    {
        JsonSchema = schema;
    }

    /// <summary>
    /// Sets a human-readable title for this schema. Titles provide a short, descriptive name
    /// that is often displayed in user interfaces, documentation, or error messages. Titles
    /// do not affect validation behavior but improve schema usability and understandability.
    /// </summary>
    /// <param name="title">The title to assign to the schema. Can be any string, including empty.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Title(string title)
    {
        JsonSchema.Title = title;
        return this;
    }

    /// <summary>
    /// Sets a detailed description explaining the purpose and intended use of this schema.
    /// Descriptions should be clear, comprehensive, and helpful for users understanding what
    /// data the schema validates. Unlike titles, descriptions can be lengthy and should cover
    /// validation constraints, expected value formats, and any special requirements.
    /// </summary>
    /// <param name="description">The description text to assign to the schema. Can be any string, including empty.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Description(string description)
    {
        JsonSchema.Description = description;
        return this;
    }

    /// <summary>
    /// Sets a unique identifier URI for this schema. The ID enables referencing this schema
    /// from $ref keywords within the same document or across different schemas. IDs should be
    /// absolute URIs when possible to avoid resolution ambiguities, though relative URIs are
    /// supported for local schema fragments.
    /// </summary>
    /// <param name="id">The URI identifier for this schema. Can be any valid URI string.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Id(string id)
    {
        JsonSchema.Id = id;
        return this;
    }

    /// <summary>
    /// Sets a short anchor identifier that can be referenced using fragment identifiers like
    /// #anchor-name within the same document. Anchors provide a lightweight alternative to full
    /// URI IDs for local schema references, particularly useful in complex schemas with many
    /// reusable definitions.
    /// </summary>
    /// <param name="anchor">The anchor name to assign to the schema. Must be a valid fragment identifier.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Anchor(string anchor)
    {
        JsonSchema.Anchor = anchor;
        return this;
    }

    /// <summary>
    /// Sets the expected data type for values validated against this schema. The type keyword
    /// restricts validation to a specific JSON type category: string, number, integer, boolean,
    /// object, array, or null. When multiple types are needed, use oneOf or anyOf compositions
    /// instead of this single-type constraint.
    /// </summary>
    /// <param name="type">The JsonValueType enum value representing the expected JSON type category.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Type(JsonPropertyType type)
    {
        JsonSchema.Type = type;
        return this;
    }

    /// <summary>
    /// Sets a format hint providing additional semantic constraints beyond the basic type.
    /// Common formats include date-time, email, uri, uuid, ipv4, and custom formats defined
    /// by specific applications. Format validation is optional, and validators may ignore
    /// unknown formats without affecting schema validity.
    /// </summary>
    /// <param name="format">The format string specifying the expected value format. Common values include date-time, email, uri, uuid, ipv4.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Format(string format)
    {
        JsonSchema.Format = format;
        return this;
    }

    /// <summary>
    /// Sets an enumeration of allowed literal values. The validated value must exactly match
    /// one of the provided values according to JSON equality semantics. This constraint is
    /// useful for restricting values to a predefined set, such as status codes, categories,
    /// or enumerated constants.
    /// </summary>
    /// <param name="values">The array of allowed literal values. Can include strings, numbers, booleans, null, or objects.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Enum(params object[] values)
    {
        JsonSchema.Enum = [.. values];
        return this;
    }

    /// <summary>
    /// Sets a single constant value that the validated data must exactly match. This is more
    /// efficient than using Enum with a single value and clearly expresses that only one
    /// specific value is permitted. The comparison uses JSON equality semantics.
    /// </summary>
    /// <param name="value">The single constant value that is the only valid option. Can be any serializable JSON value.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Const(object? value)
    {
        JsonSchema.Const = value;
        return this;
    }

    /// <summary>
    /// Sets a positive numeric constraint requiring validated numbers to be divisible by the
    /// specified value. The validation passes when dividing the value by MultipleOf results
    /// in an integer with no remainder. This is commonly used for constraints like "must be
    /// a multiple of 100" for currency amounts or "must be divisible by 60" for time intervals.
    /// </summary>
    /// <param name="value">The divisor value, which must be positive (> 0). Values of zero or negative will cause validation errors.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is not a positive finite number (&lt;=0, NaN, or Infinity).</exception>
    public JsonSchemaBuilder MultipleOf(double value)
    {
        if (value <= 0 || double.IsInfinity(value) || double.IsNaN(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "MultipleOf must be a positive finite number.");
        }

        JsonSchema.MultipleOf = value;
        return this;
    }

    /// <summary>
    /// Sets a numeric upper bound constraint with optional exclusivity. When exclusive is false
    /// (default), validated values must be less than or equal to the specified maximum. When
    /// exclusive is true, values must be strictly less than the maximum, making it an open
    /// upper bound. This method handles both Maximum and ExclusiveMaximum keywords efficiently.
    /// </summary>
    /// <param name="value">The maximum numeric value. Must be a finite number (not NaN or Infinity).</param>
    /// <param name="exclusive">Whether to use exclusive comparison. False for less than or equal, True for strictly less than.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is not a finite number (NaN or Infinity).</exception>
    public JsonSchemaBuilder Maximum(double value, bool exclusive = false)
    {
        if (double.IsInfinity(value) || double.IsNaN(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Maximum must be a finite number.");
        }

        if (exclusive)
        {
            JsonSchema.ExclusiveMaximum = value;
        }
        else
        {
            JsonSchema.Maximum = value;
        }

        return this;
    }

    /// <summary>
    /// Sets an exclusive numeric upper bound, requiring validated values to be strictly less
    /// than the specified maximum. This provides an open upper bound where the maximum value
    /// itself is not valid. Use this instead of Maximum with exclusive=true for clarity.
    /// </summary>
    /// <param name="value">The exclusive maximum numeric value. Must be a finite number (not NaN or Infinity).</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is not a finite number (NaN or Infinity).</exception>
    public JsonSchemaBuilder ExclusiveMaximum(double value)
    {
        if (double.IsInfinity(value) || double.IsNaN(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "ExclusiveMaximum must be a finite number.");
        }

        JsonSchema.ExclusiveMaximum = value;
        return this;
    }

    /// <summary>
    /// Sets a numeric lower bound constraint with optional exclusivity. When exclusive is false
    /// (default), validated values must be greater than or equal to the specified minimum. When
    /// exclusive is true, values must be strictly greater than the minimum, making it an open
    /// lower bound. This method handles both Minimum and ExclusiveMinimum keywords efficiently.
    /// </summary>
    /// <param name="value">The minimum numeric value. Must be a finite number (not NaN or Infinity).</param>
    /// <param name="exclusive">Whether to use exclusive comparison. False for >=, True for >.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is not a finite number (NaN or Infinity).</exception>
    public JsonSchemaBuilder Minimum(double value, bool exclusive = false)
    {
        if (double.IsInfinity(value) || double.IsNaN(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Minimum must be a finite number.");
        }

        if (exclusive)
        {
            JsonSchema.ExclusiveMinimum = value;
        }
        else
        {
            JsonSchema.Minimum = value;
        }

        return this;
    }

    /// <summary>
    /// Sets an exclusive numeric lower bound, requiring validated values to be strictly greater
    /// than the specified minimum. This provides an open lower bound where the minimum value
    /// itself is not valid. Use this instead of Minimum with exclusive=true for clarity.
    /// </summary>
    /// <param name="value">The exclusive minimum numeric value. Must be a finite number (not NaN or Infinity).</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is not a finite number (NaN or Infinity).</exception>
    public JsonSchemaBuilder ExclusiveMinimum(double value)
    {
        if (double.IsInfinity(value) || double.IsNaN(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "ExclusiveMinimum must be a finite number.");
        }

        JsonSchema.ExclusiveMinimum = value;
        return this;
    }

    /// <summary>
    /// Sets the maximum allowed length for string values in characters. Validated strings must
    /// contain no more characters than the specified limit. This constraint only applies to
    /// string types and is commonly used for field length restrictions like passwords or usernames.
    /// </summary>
    /// <param name="value">The maximum string length in characters. Must be a non-negative integer.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder MaxLength(int value)
    {
        JsonSchema.MaxLength = value;
        return this;
    }

    /// <summary>
    /// Sets the minimum allowed length for string values in characters. Validated strings must
    /// contain at least the specified number of characters. This constraint only applies to
    /// string types and is commonly used for field length requirements like minimum password
    /// length or mandatory text content.
    /// </summary>
    /// <param name="value">The minimum string length in characters. Must be a non-negative integer.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder MinLength(int value)
    {
        JsonSchema.MinLength = value;
        return this;
    }

    /// <summary>
    /// Sets a regular expression pattern for string validation. Validated strings must match
    /// the specified pattern according to ECMA-262 regex semantics. Patterns should be tested
    /// thoroughly before deployment as invalid patterns can cause validation failures or security
    /// issues. Common uses include email validation, ID formats, and content restrictions.
    /// Security Note: To protect against ReDoS (Regular Expression Denial of Service) attacks,
    /// avoid patterns with nested quantifiers like (a+)+ or excessive backtracking. Keep patterns
    /// simple and well-tested. The 10,000 character limit helps mitigate this risk.
    /// </summary>
    /// <param name="pattern">The regular expression pattern string. Must be a valid ECMA-262 regex.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    /// <exception cref="ArgumentNullException">Thrown when pattern is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when pattern is an invalid regular expression.</exception>
    public JsonSchemaBuilder Pattern(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            throw new ArgumentNullException(nameof(pattern), "Pattern cannot be null or empty.");
        }

        JsonSchema.Pattern = pattern;
        return this;
    }

    /// <summary>
    /// Sets the maximum allowed number of elements in an array. Validated arrays must contain
    /// no more items than the specified limit. This constraint only applies to array types and
    /// works in conjunction with MinItems to define acceptable array size ranges.
    /// </summary>
    /// <param name="value">The maximum array length in elements. Must be a non-negative integer.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder MaxItems(int value)
    {
        JsonSchema.MaxItems = value;
        return this;
    }

    /// <summary>
    /// Sets the minimum allowed number of elements in an array. Validated arrays must contain
    /// at least the specified number of items. This constraint only applies to array types and
    /// works in conjunction with MaxItems to define acceptable array size ranges.
    /// </summary>
    /// <param name="value">The minimum array length in elements. Must be a non-negative integer.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder MinItems(int value)
    {
        JsonSchema.MinItems = value;
        return this;
    }

    /// <summary>
    /// Sets whether all array elements must be unique. When true, no two items in the validated
    /// array can be equal according to JSON equality semantics. This is useful for validating
    /// collections where duplicates are not allowed, such as sets of identifiers or unique tags.
    /// </summary>
    /// <param name="value">Whether uniqueness is required. True enforces unique items, False allows duplicates.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder UniqueItems(bool value = true)
    {
        JsonSchema.UniqueItems = value;
        return this;
    }

    /// <summary>
    /// Configures a containment constraint requiring at least one array element to match the
    /// specified subschema. Unlike Items which validates all elements, Contains provides existential
    /// validation, ensuring the array contains at least one valid item of a particular type or
    /// structure. This is useful for validating arrays that must include specific elements.
    /// </summary>
    /// <param name="configure">A lambda function that configures the containment subschema. The function receives a nested builder and returns the configured schema.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Contains(Func<JsonSchemaBuilder, JsonSchemaBuilder> configure)
    {
        JsonSchema.Contains = configure(Create()).Build();
        return this;
    }

    /// <summary>
    /// Sets the maximum allowed number of properties in an object. Validated objects must contain
    /// no more property keys than the specified limit. This constraint only applies to object
    /// types and works in conjunction with MinProperties to define acceptable object size ranges.
    /// </summary>
    /// <param name="value">The maximum number of object properties. Must be a non-negative integer.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder MaxProperties(int value)
    {
        JsonSchema.MaxProperties = value;
        return this;
    }

    /// <summary>
    /// Sets the minimum allowed number of properties in an object. Validated objects must contain
    /// at least the specified number of property keys. This constraint only applies to object
    /// types and works in conjunction with MaxProperties to define acceptable object size ranges.
    /// </summary>
    /// <param name="value">The minimum number of object properties. Must be a non-negative integer.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder MinProperties(int value)
    {
        JsonSchema.MinProperties = value;
        return this;
    }

    /// <summary>
    /// Adds one or more required property names to the schema. Validated objects must contain
    /// all specified properties regardless of whether they have null values. This method can be
    /// called multiple times to accumulate required properties, and it initializes the Required
    /// list if it hasn't been created yet.
    /// </summary>
    /// <param name="properties">The array of property names that must be present in validated objects.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Required(params string[] properties)
    {
        JsonSchema.Required ??= [];
        JsonSchema.Required.AddRange(properties);
        return this;
    }

    /// <summary>
    /// Adds a property with its associated subschema to the Properties dictionary. The property
    /// name maps to a schema that defines validation rules for that specific property. This is
    /// the simpler overload when you already have a configured JsonSchema instance. For building
    /// nested schemas inline, use the Func&lt;SchemaBuilder, SchemaBuilder&gt; overload instead.
    /// </summary>
    /// <param name="name">The property name to add. Must be a valid JSON property name.</param>
    /// <param name="schema">The JsonSchema instance defining validation rules for this property.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Property(string name, JsonSchema schema)
    {
        JsonSchema.Properties ??= [];
        JsonSchema.Properties[name] = schema;
        return this;
    }

    /// <summary>
    /// Adds a property with an inline configured subschema to the Properties dictionary. This
    /// overload accepts a lambda function that receives a nested builder for configuring the
    /// property's schema without creating intermediate objects. The resulting schema is automatically
    /// added to the Properties dictionary under the specified name.
    /// </summary>
    /// <param name="name">The property name to add. Must be a valid JSON property name.</param>
    /// <param name="configure">A lambda function that configures the property subschema. The function receives a nested builder and returns the configured schema.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Property(string name, Func<JsonSchemaBuilder, JsonSchemaBuilder> configure)
    {
        JsonSchema.Properties ??= [];
        JsonSchema.Properties[name] = configure(Create()).Build();
        return this;
    }

    /// <summary>
    /// Adds a pattern-based property mapping where object properties matching the regex pattern
    /// are validated against the associated subschema. This is useful for validating dynamic
    /// property names like email addresses, IDs, or structured keys without enumerating each
    /// possible property name explicitly.
    /// </summary>
    /// <param name="pattern">The regular expression pattern for matching property names.</param>
    /// <param name="schema">The JsonSchema instance defining validation rules for properties matching the pattern.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder PatternProperty(string pattern, JsonSchema schema)
    {
        JsonSchema.PatternProperties ??= [];
        JsonSchema.PatternProperties[pattern] = schema;
        return this;
    }

    /// <summary>
    /// Adds a pattern-based property mapping with an inline configured subschema. This overload
    /// accepts a lambda function for configuring the validation schema without creating intermediate
    /// objects. Properties whose names match the specified pattern will be validated against
    /// the resulting schema.
    /// </summary>
    /// <param name="pattern">The regular expression pattern for matching property names.</param>
    /// <param name="configure">A lambda function that configures the pattern property subschema. The function receives a nested builder and returns the configured schema.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder PatternProperty(string pattern, Func<JsonSchemaBuilder, JsonSchemaBuilder> configure)
    {
        JsonSchema.PatternProperties ??= [];
        JsonSchema.PatternProperties[pattern] = configure(Create()).Build();
        return this;
    }

    /// <summary>
    /// Configures whether additional properties (those not defined in Properties) are allowed.
    /// When allowed is true, additional properties are permitted without validation. When false,
    /// additional properties are forbidden and will cause validation failures. This provides
    /// a quick way to control permissive vs restrictive object validation behavior.
    /// </summary>
    /// <param name="allowed">Whether additional properties are permitted. True allows them, False forbids them.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder AdditionalProperties(bool allowed)
    {
        JsonSchema.AdditionalProperties = allowed ? null : new JsonSchema();
        return this;
    }

    /// <summary>
    /// Sets a subschema for validating additional properties not defined in the Properties dictionary.
    /// Additional properties that match this schema are allowed, while those that don't match will
    /// fail validation. This provides fine-grained control over what extra properties are acceptable.
    /// </summary>
    /// <param name="schema">The JsonSchema instance defining validation rules for additional properties.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder AdditionalProperties(JsonSchema schema)
    {
        JsonSchema.AdditionalProperties = schema;
        return this;
    }

    /// <summary>
    /// Sets a subschema for validating additional properties using an inline configured builder.
    /// This overload accepts a lambda function for configuring the additional properties schema
    /// without creating intermediate objects. Any property not explicitly defined in Properties
    /// will be validated against this schema.
    /// </summary>
    /// <param name="configure">A lambda function that configures the additional properties subschema. The function receives a nested builder and returns the configured schema.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder AdditionalProperties(Func<JsonSchemaBuilder, JsonSchemaBuilder> configure)
    {
        JsonSchema.AdditionalProperties = configure(Create()).Build();
        return this;
    }

    /// <summary>
    /// Sets a subschema that validates all items in an array. All array elements must match this
    /// schema for validation to pass. This provides uniform validation for all array items. For
    /// tuple validation with different types per position, use PrefixItems instead.
    /// </summary>
    /// <param name="schema">The JsonSchema instance defining validation rules for array items.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Items(JsonSchema schema)
    {
        JsonSchema.Items = schema;
        return this;
    }

    /// <summary>
    /// Sets a subschema for validating all array items using an inline configured builder. This
    /// overload accepts a lambda function for configuring the items schema without creating
    /// intermediate objects. All array elements will be validated against the resulting schema.
    /// </summary>
    /// <param name="configure">A lambda function that configures the items subschema. The function receives a nested builder and returns the configured schema.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Items(Func<JsonSchemaBuilder, JsonSchemaBuilder> configure)
    {
        JsonSchema.Items = configure(Create()).Build();
        return this;
    }

    /// <summary>
    /// Sets an array of subschemas for tuple validation where each position has a specific type.
    /// Each element in the validated array is validated against the corresponding schema in this
    /// array by position. Positions beyond the array length are allowed unless AdditionalItems
    /// is specified. This enables strict positional type checking for fixed-length arrays.
    /// </summary>
    /// <param name="schemas">The array of lambda functions, each configuring a subschema for a specific array position.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder PrefixItems(params Func<JsonSchemaBuilder, JsonSchemaBuilder>[] schemas)
    {
        List<JsonSchema> items = new(schemas.Length);
        foreach (Func<JsonSchemaBuilder, JsonSchemaBuilder> schema in schemas)
        {
            items.Add(schema(Create()).Build());
        }
        JsonSchema.PrefixItems = items;
        return this;
    }

    /// <summary>
    /// Sets a reference URI pointing to another schema definition. The validated value defers
    /// validation to the referenced schema, which can be a relative URI, absolute URI, or fragment
    /// identifier. References enable schema reuse and modular schema design across documents.
    /// </summary>
    /// <param name="reference">The reference URI string. Can be relative, absolute, or a fragment identifier.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Ref(string reference)
    {
        JsonSchema.Ref = reference;
        return this;
    }

    /// <summary>
    /// Adds a definition to the $defs dictionary that can be referenced using fragment URIs like
    /// #/$defs/{name}. Definitions are reusable schema fragments that avoid duplication and organize
    /// complex schemas. The name parameter becomes part of the reference URI when referencing
    /// this definition from other parts of the schema.
    /// </summary>
    /// <param name="name">The definition name that will be used in fragment references like #/$defs/{name}.</param>
    /// <param name="configure">A lambda function that configures the definition subschema. The function receives a nested builder and returns the configured schema.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Def(string name, Func<JsonSchemaBuilder, JsonSchemaBuilder> configure) =>
        Def(name, configure(Create()).Build());

    /// <summary>
    /// Adds a definition to the $defs dictionary that can be referenced using fragment URIs like
    /// #/$defs/{name}. Definitions are reusable schema fragments that avoid duplication and organize
    /// complex schemas. The name parameter becomes part of the reference URI when referencing
    /// this definition from other parts of the schema.
    /// </summary>
    /// <param name="name">The definition name that will be used in fragment references like #/$defs/{name}.</param>
    /// <param name="schema">The schema to add to defs.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Def(string name, JsonSchema schema)
    {
        JsonSchema.Defs ??= [];
        JsonSchema.Defs[name] = schema;
        return this;
    }

    /// <summary>
    /// Adds a single subschema to the AllOf array for intersection validation. The validated value
    /// must satisfy all schemas in the AllOf array, including this one. This method allows incremental
    /// addition of AllOf components through method chaining. For adding multiple schemas at once,
    /// use the params overload instead.
    /// </summary>
    /// <param name="schema">A lambda function that configures the subschema to add to AllOf. The function receives a nested builder and returns the configured schema.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder AllOf(Func<JsonSchemaBuilder, JsonSchemaBuilder> schema)
    {
        JsonSchema.AllOf ??= [];
        JsonSchema.AllOf.Add(schema(Create()).Build());
        return this;
    }

    /// <summary>
    /// Adds multiple subschemas to the AllOf array for intersection validation in a single call.
    /// The validated value must satisfy all schemas in the AllOf array. This is more efficient
    /// than calling the single-schema overload multiple times when adding several schemas at once.
    /// </summary>
    /// <param name="schemas">The array of lambda functions, each configuring a subschema to add to AllOf.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder AllOf(params Func<JsonSchemaBuilder, JsonSchemaBuilder>[] schemas)
    {
        List<JsonSchema> items = new(schemas.Length);
        foreach (Func<JsonSchemaBuilder, JsonSchemaBuilder> schema in schemas)
        {
            items.Add(schema(Create()).Build());
        }
        JsonSchema.AllOf = items;
        return this;
    }

    /// <summary>
    /// Sets an array of subschemas where the validated value must match at least one of them.
    /// AnyOf provides union validation, accepting values that satisfy any schema in the array.
    /// This is useful for accepting multiple valid formats or types for the same semantic value,
    /// such as allowing both string and number representations of a quantity.
    /// </summary>
    /// <param name="schemas">The array of lambda functions, each configuring a subschema for AnyOf validation.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder AnyOf(params Func<JsonSchemaBuilder, JsonSchemaBuilder>[] schemas)
    {
        List<JsonSchema> items = new(schemas.Length);
        foreach (Func<JsonSchemaBuilder, JsonSchemaBuilder> schema in schemas)
        {
            items.Add(schema(Create()).Build());
        }
        JsonSchema.AnyOf = items;
        return this;
    }

    /// <summary>
    /// Sets an array of subschemas where the validated value must match exactly one of them.
    /// OneOf provides exclusive union validation, stricter than AnyOf because it requires precisely
    /// one schema to match (not zero, not multiple). This is useful for discriminated unions where
    /// only one variant should be valid at a time.
    /// </summary>
    /// <param name="schemas">The array of lambda functions, each configuring a subschema for OneOf validation.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder OneOf(params Func<JsonSchemaBuilder, JsonSchemaBuilder>[] schemas)
    {
        List<JsonSchema> items = new(schemas.Length);
        foreach (Func<JsonSchemaBuilder, JsonSchemaBuilder> schema in schemas)
        {
            items.Add(schema(Create()).Build());
        }
        JsonSchema.OneOf = items;
        return this;
    }

    /// <summary>
    /// Sets a subschema that the validated value must not match. Not provides negation validation,
    /// rejecting values that satisfy this schema while accepting all others. This is useful for
    /// excluding specific values or patterns from an otherwise permissive schema, such as blocking
    /// reserved words or invalid states.
    /// </summary>
    /// <param name="configure">A lambda function that configures the negation subschema. The function receives a nested builder and returns the configured schema.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Not(Func<JsonSchemaBuilder, JsonSchemaBuilder> configure)
    {
        JsonSchema.Not = configure(Create()).Build();
        return this;
    }

    /// <summary>
    /// Configures conditional validation using the if-then-else construct. When the validated value
    /// matches the If schema, it must also match the Then schema (if provided). When the value doesn't
    /// match the If schema, it must match the Else schema (if provided). This enables dynamic validation
    /// rules that change based on data content or structure.
    /// </summary>
    /// <param name="ifSchema">A lambda function that configures the conditional check schema. The function receives a nested builder and returns the If schema.</param>
    /// <param name="thenSchema">An optional lambda function that configures the then subschema. Required only if you want to specify validation when the condition is true.</param>
    /// <param name="elseSchema">An optional lambda function that configures the else subschema. Required only if you want to specify validation when the condition is false.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder IfThenElse(
        Func<JsonSchemaBuilder, JsonSchemaBuilder> ifSchema,
        Func<JsonSchemaBuilder, JsonSchemaBuilder>? thenSchema = null,
        Func<JsonSchemaBuilder, JsonSchemaBuilder>? elseSchema = null)
    {
        JsonSchema.If = ifSchema(Create()).Build();
        if (thenSchema is not null)
        {
            JsonSchema.Then = thenSchema(Create()).Build();
        }

        if (elseSchema is not null)
        {
            JsonSchema.Else = elseSchema(Create()).Build();
        }

        return this;
    }

    /// <summary>
    /// Adds a custom extension keyword that is not part of the JSON Schema specification. Extensions
    /// allow adding vendor-specific validation logic or metadata while maintaining compatibility with
    /// standard validators that ignore unknown keywords. Use this sparingly and document the extension
    /// purpose clearly, as extensions reduce portability across different validation implementations.
    /// </summary>
    /// <param name="key">The extension keyword name. Should follow naming conventions like x- for vendor extensions.</param>
    /// <param name="value">The value for the extension keyword. Can be any serializable JSON value including null.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder CustomKeyword(string key, object? value)
    {
        JsonSchema.AddExtension(key, value);
        return this;
    }

    /// <summary>
    /// Adds a custom extension keyword using a configuration function. This allows building
    /// nested extension structures while maintaining the fluent builder pattern.
    /// </summary>
    /// <param name="key">The extension keyword name.</param>
    /// <param name="configure">A function that configures a new SchemaBuilder for the extension value.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder CustomKeyword(string key, Func<JsonSchemaBuilder, JsonSchemaBuilder>? configure)
    {
        if (configure is null)
        {
            JsonSchema.Extensions ??= [];
            JsonSchema.Extensions[key] = null;
        }
        else
        {
            JsonSchema.AddExtension(key, configure(Create()).Build());
        }
        return this;
    }

    /// <summary>
    /// Configures a discriminator object for selecting among multiple schemas in oneOf or anyOf compositions.
    /// The discriminator specifies which property name to inspect to determine which schema variant should
    /// be applied, and optionally provides a mapping from property values to schema references. This is
    /// essential for polymorphic type hierarchies in OpenAPI and other specifications.
    /// </summary>
    /// <param name="propertyName">The name of the property used as the discriminator key.</param>
    /// <param name="mapping">An optional dictionary mapping discriminator property values to schema references. Each key is a possible value, and each value is a URI fragment pointing to the corresponding schema.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder Discriminator(string propertyName, Dictionary<string, string>? mapping = null)
    {
        JsonSchema.Discriminator = new()
        {
            PropertyName = propertyName,
            Mapping = mapping
        };
        return this;
    }

    /// <summary>
    /// Sets the .NET type name for this schema. This property is used internally by the schema generator
    /// to track the source type and is not serialized to JSON. Setting this manually allows correlating
    /// generated schemas back to their original .NET types for debugging and inspection purposes.
    /// </summary>
    /// <param name="typeName">The .NET type name to assign, or null to clear.</param>
    /// <returns>This SchemaBuilder instance to enable method chaining for fluent schema construction</returns>
    public JsonSchemaBuilder TypeName(string? typeName)
    {
        JsonSchema.TypeName = typeName;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured JsonSchema instance. This finalizes all accumulated configuration
    /// and produces the immutable schema object ready for serialization or validation. After calling Build(),
    /// the SchemaBuilder cannot be reused as it has yielded its result. For building multiple similar schemas,
    /// create new builder instances using Create().
    /// </summary>
    /// <returns>The fully configured JsonSchema instance with all applied validations and constraints</returns>
    public JsonSchema Build()
    {
        return JsonSchema;
    }
}
