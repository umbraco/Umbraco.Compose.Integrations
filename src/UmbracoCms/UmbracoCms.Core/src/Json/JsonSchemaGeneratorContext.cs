using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

/// <summary>
/// Provides context for JSON schema generation, maintaining state during the generation process.
/// This class tracks registered schemas, manages type-to-schema mappings, and coordinates
/// between custom handlers and the default generator. Use this class when you need fine-grained
/// control over schema generation or want to reuse generation state across multiple type schemas.
/// </summary>
public sealed class JsonSchemaGeneratorContext
{
    private readonly Dictionary<Type, JsonSchema> _schemasByType = [];
    private readonly Dictionary<string, JsonSchema> _schemasByName = [];
    private JsonSchemaBuilder? _rootSchemaBuilder;

    internal TypeInfo? CurrentHandling { get; set; }

    /// <summary>
    /// Gets the configuration options controlling schema generation behavior. These options determine
    /// reference handling modes, property naming policies, custom type handlers, and the type name
    /// generator used throughout schema creation. The options are initialized from the constructor
    /// parameter or default settings if null is provided.
    /// </summary>
    public JsonSchemaGeneratorOptions Options { get; }

    /// <summary>
    /// Gets an immutable snapshot of all registered schemas indexed by their type names. This dictionary
    /// contains schemas that have been successfully registered during generation and can be used to
    /// look up schemas by name for reference resolution or inspection. The collection is read-only
    /// to prevent accidental modification of the generation state.
    /// </summary>
    public IReadOnlyDictionary<string, JsonSchema> Schemas =>
        _schemasByName.AsReadOnly();

    /// <summary>
    /// Gets the root schema that was created during generation, if any. The root schema represents
    /// the primary schema generated for the initial type and may contain references to other schemas
    /// in $defs when using Defs reference mode. This property is null until a schema has been
    /// generated and registered as the root.
    /// </summary>
    public JsonSchema? RootSchema { get; private set; }

    /// <summary>
    /// Initializes a new JsonSchemaGeneratorContext instance with optional custom configuration.
    /// When no options are provided, default settings are used including Inline reference mode and
    /// an empty handler list. This constructor is the recommended entry point for creating contexts
    /// when you need full control over schema generation state.
    /// </summary>
    /// <param name="options">Optional JsonSchemaGeneratorOptions for customizing generation behavior. If null, default options are used.</param>
    public JsonSchemaGeneratorContext(JsonSchemaGeneratorOptions? options = default)
    {
        Options = options ?? JsonSchemaGeneratorOptions.Default;
    }

    /// <summary>
    /// Registers a schema by its explicit name in the generation context. This method adds the schema
    /// to both type-based and name-based lookup dictionaries, enabling reference resolution by either
    /// the Type object or the string name. The name is used as the key for the name-based dictionary
    /// and for generating fragment references like #/$defs/{name}. Throws an exception if registration
    /// fails due to duplicate names or types.
    /// </summary>
    /// <param name="name">The unique name for this schema. Must be a non-empty string.</param>
    /// <param name="schema">The JsonSchema instance to register. Must be a non-null object.</param>
    /// <exception cref="ArgumentNullException">Thrown when schema is null or name is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a schema with the same name or type already exists.</exception>
    public void RegisterSchema(string name, JsonSchema schema)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(schema);

        if (TryRegisterSchema(schema, schema.ClrType, name))
        {
            return;
        }

        throw new InvalidOperationException("Could not register schema");
    }

    /// <summary>
    /// Registers a schema using the type's generated name from the TypeNameGenerator. This convenience
    /// method automatically determines the schema name by calling Options.TypeNameGenerator.GenerateName
    /// on the provided type, then registers the schema with that name. Use this when you want to register
    /// schemas based on their .NET types without manually specifying names.
    /// </summary>
    /// <param name="type">The .NET Type to generate a name for and register. Must be a non-null object.</param>
    /// <param name="schema">The JsonSchema instance to register. Must be a non-null object.</param>
    /// <exception cref="ArgumentNullException">Thrown when schema is null or type is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a schema with the same name or type already exists.</exception>
    public void RegisterSchema(Type type, JsonSchema schema)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(schema);

        string typeName = Options.TypeNameGenerator.GenerateName(type);

        if (TryRegisterSchema(schema, type, typeName))
        {
            return;
        }

        throw new InvalidOperationException("Could not register schema");
    }

    /// <summary>
    /// Registers a schema with explicit control over both the Type and name mappings. This method provides
    /// full flexibility for registering schemas with custom names that may differ from the type's generated
    /// name. Use this when you need to override default naming or register schemas for types without
    /// associated .NET Type objects.
    /// </summary>
    /// <param name="schema">The JsonSchema instance to register. Must be a non-null object.</param>
    /// <param name="type">The .NET Type associated with this schema, or null if no type association exists.</param>
    /// <param name="name">The unique name for this schema. Must be a non-empty string.</param>
    /// <exception cref="ArgumentNullException">Thrown when schema, type, or name is null/empty as appropriate.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a schema with the same name or type already exists.</exception>
    public void RegisterSchema(JsonSchema schema, Type type, string name)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (TryRegisterSchema(schema, type, name))
        {
            return;
        }

        throw new InvalidOperationException("Could not register schema");
    }

    /// <summary>
    /// Attempts to register a schema in both type-based and name-based lookup dictionaries. This internal
    /// method handles the registration logic and returns false if either dictionary already contains an
    /// entry for the given key, preventing duplicate registrations. When registration succeeds, it also
    /// adds the schema as a definition to the root schema if using Defs reference mode. This method is
    /// thread-safe for single-threaded usage but does not provide synchronization for concurrent access.
    /// </summary>
    /// <param name="schema">The JsonSchema instance to register. Must be a non-null object.</param>
    /// <param name="type">The .NET Type associated with this schema, or null if no type association exists.</param>
    /// <param name="name">The unique name for this schema in the name-based dictionary.</param>
    /// <returns>True if registration succeeded, false if a duplicate entry exists for the type or name.</returns>
    /// <exception cref="ArgumentNullException">Thrown when schema is null.</exception>
    public bool TryRegisterSchema(JsonSchema schema, Type? type, string name)
    {
        ArgumentNullException.ThrowIfNull(schema);

        if (type is not null && !_schemasByType.TryAdd(type, schema))
        {
            return false;
        }
        if (!_schemasByName.TryAdd(name, schema))
        {
            if (type is not null)
            {
                _schemasByType.Remove(type);
            }
            return false;
        }

        if (_rootSchemaBuilder is null
            || RootSchema == schema
            || schema.Type != JsonPropertyType.Object
            || Options.ReferenceMode != ReferenceMode.Defs)
        {
            return true;
        }

        _rootSchemaBuilder.Def(name, schema);

        return true;
    }

    /// <summary>
    /// Checks whether a schema has been registered for the specified type. This method performs a direct
    /// lookup in the type-based dictionary without generating names or performing any transformations.
    /// Use this when you need to quickly determine if a type's schema is already available before
    /// attempting to generate or retrieve it.
    /// </summary>
    /// <param name="type">The .NET Type to check for registration. Must be a non-null object.</param>
    /// <returns>True if a schema exists for the type, false otherwise.</returns>
    public bool ContainsType(Type type) =>
        _schemasByType.ContainsKey(type);

    /// <summary>
    /// Checks whether a schema has been registered under the specified name. This method performs a direct
    /// lookup in the name-based dictionary without any name generation or transformations. Use this when
    /// you need to verify if a named schema is available before attempting to reference it.
    /// </summary>
    /// <param name="name">The schema name to check for registration. Must be a non-null string.</param>
    /// <returns>True if a schema exists with the given name, false otherwise.</returns>
    public bool ContainsType(string name) =>
        _schemasByName.ContainsKey(name);

    /// <summary>
    /// Attempts to retrieve a schema by its associated Type object. This method performs a direct lookup
    /// in the type-based dictionary and returns the registered schema if found, or null if no schema exists
    /// for the type. This is the preferred method for retrieving schemas when you have the Type object
    /// and want to avoid name generation overhead.
    /// </summary>
    /// <param name="type">The .NET Type to look up. Must be a non-null object.</param>
    /// <param name="schema">When this method returns, contains the registered JsonSchema for the type if found, or null otherwise.</param>
    /// <returns>True if a schema was found for the type, false if no schema exists.</returns>
    public bool TryGetSchema(Type type, [NotNullWhen(true)] out JsonSchema? schema) =>
        _schemasByType.TryGetValue(type, out schema);

    /// <summary>
    /// Attempts to retrieve a schema by its registered name. This method performs a direct lookup in the
    /// name-based dictionary and returns the registered schema if found, or null if no schema exists with
    /// that name. Use this when you need to resolve schema references using string names rather than Type objects.
    /// </summary>
    /// <param name="name">The schema name to look up. Must be a non-null string.</param>
    /// <param name="schema">When this method returns, contains the registered JsonSchema for the name if found, or null otherwise.</param>
    /// <returns>True if a schema was found with the given name, false if no schema exists.</returns>
    public bool TryGetSchema(string name, [NotNullWhen(true)] out JsonSchema? schema) =>
        _schemasByName.TryGetValue(name, out schema);

    /// <summary>
    /// Creates a new JsonSchemaBuilder instance configured with the specified type and optional schema URI.
    /// This method initializes a builder for constructing schemas and tracks it as the root schema builder
    /// for the generation context. For object types, it also sets the default schema URI from Options if
    /// not explicitly provided. The returned builder can be used to configure and build schemas that will
    /// be automatically tracked in the context's registration dictionaries.
    /// </summary>
    /// <param name="type">The JsonPropertyType to initialize the builder with. This determines the base type of the schema being built.</param>
    /// <param name="schema">Optional schema URI to set on object types. If null, Options.DefaultSchema is used for object types.</param>
    /// <returns>A new JsonSchemaBuilder instance configured with the specified type and schema URI.</returns>
    public JsonSchemaBuilder CreateBuilder(JsonPropertyType type, string? schema = default)
    {
        JsonSchemaBuilder builder = new();
        builder.Type(type);

        if (type is JsonPropertyType.Object)
        {
            builder.Schema(schema ?? Options.DefaultSchema);
        }

        _rootSchemaBuilder ??= builder;
        RootSchema ??= builder.JsonSchema;

        return builder;
    }

    /// <summary>
    /// Generates a JSON Schema for the specified generic type T using the current context configuration.
    /// This convenience method delegates to the Type-based Generate method by inferring the type from
    /// the generic parameter. Use this when you have a compile-time known type and want concise syntax
    /// for schema generation with full context support.
    /// </summary>
    /// <typeparam name="T">The .NET type to generate a schema for. Can be any serializable type.</typeparam>
    /// <returns>A JsonSchema instance representing the structure and constraints of type T.</returns>
    public JsonSchema Generate<T>() =>
        Generate(typeof(T));

    /// <summary>
    /// Generates a JSON Schema for the specified runtime type using the current context configuration.
    /// This method analyzes the type structure including properties, nested types, and collections, then
    /// produces a comprehensive schema with appropriate references based on the context's reference mode.
    /// The generated schema validates data against the .NET type contract and is automatically registered
    /// in the context for future reference resolution.
    /// </summary>
    /// <param name="type">The .NET Type object to generate a schema for. Must be a valid serializable type.</param>
    /// <returns>A JsonSchema instance representing the structure and constraints of the specified type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
    public JsonSchema Generate(Type type) =>
        JsonSchemaGenerator.Generate(type, this);

    /// <summary>
    /// Attempts to find a custom type handler that can handle the specified type. This method iterates
    /// through the registered handlers in Options.Handlers and returns the first handler whose CanHandle
    /// method returns true for the given type. Use this when you need to determine which handler will
    /// be used for a specific type or when implementing custom schema generation logic.
    /// </summary>
    /// <param name="type">The .NET Type to find a handler for. Must be a non-null object.</param>
    /// <param name="handler">When this method returns, contains the matching IJsonSchemaTypeHandler if found, or null otherwise.</param>
    /// <returns>True if a handler was found that can handle the type, false if no handlers match.</returns>
    /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
    public bool TryFindHandler(Type type, [NotNullWhen(true)] out IJsonSchemaTypeHandler? handler)
    {
        ArgumentNullException.ThrowIfNull(type);

        handler = Options.Handlers.FirstOrDefault(x => x.CanHandle(type));
        return handler is not null;
    }

    /// <summary>
    /// Gets the generated type name for the specified Type using either a custom handler or the configured
    /// TypeNameGenerator. This method first checks if a custom handler exists for the type and uses its
    /// GetTypeName method if available, otherwise delegates to Options.TypeNameGenerator.GenerateName.
    /// Use this when you need to obtain the name that will be used for schema references and definitions.
    /// </summary>
    /// <param name="type">The .NET Type to generate a name for. Must be a non-null object.</param>
    /// <returns>The generated type name that will be used for schema references and definitions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
    internal string GetTypeName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (TryFindHandler(type, out IJsonSchemaTypeHandler? handler))
        {
            return handler.GetTypeName(this, type);
        }

        return this.Options.TypeNameGenerator.GenerateName(type);
    }
}
