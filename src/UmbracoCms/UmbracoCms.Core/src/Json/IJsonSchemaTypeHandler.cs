namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

/// <summary>
/// Defines the contract for custom type handlers that extend JSON Schema generation to non-standard
/// .NET types. Implement this interface to register custom handlers with JsonSchemaGenerator for
/// types that require special schema generation logic beyond the built-in type support. Handlers
/// are checked in registration order, and the first matching handler is used for schema creation.
/// </summary>
public interface IJsonSchemaTypeHandler
{
    /// <summary>
    /// Determines whether this handler can produce a JSON Schema for the specified type. This
    /// method is called for each type during generation, and handlers should return true only
    /// for types they know how to handle. Return false for unsupported types to allow other
    /// handlers or the default generator to process them.
    /// </summary>
    /// <param name="type">The .NET Type to evaluate</param>
    /// <returns>True if this handler can generate a schema for the type, false otherwise</returns>
    bool CanHandle(Type type);

    /// <summary>
    /// Generates a JSON Schema for the specified type. This method is called only after CanHandle
    /// returns true, guaranteeing that the handler supports the requested type. Return null to
    /// signal that despite claiming to handle the type, generation failed and the type should be
    /// skipped. Otherwise, return a complete JsonSchema instance representing the type structure.
    /// </summary>
    /// <param name="context">The JsonSchemaGeneratorContext providing access to registration methods and options.</param>
    /// <param name="type">The .NET Type to generate a schema for, guaranteed to be supported by this handler</param>
    /// <returns>A JsonSchema instance representing the type, or null if generation fails.</returns>
    JsonSchema Handle(JsonSchemaGeneratorContext context, Type type);

    /// <summary>
    /// Gets the name to use for the specified type in schema definitions and references. This method
    /// is called during schema generation to produce names for use in $defs dictionaries and fragment
    /// URIs like #/$defs/{name}. The returned name should be unique within the schema generation context
    /// and suitable for use as a JSON property name. Custom handlers can implement custom naming logic
    /// to override the default TypeNameGenerator behavior for their specific types.
    /// </summary>
    /// <param name="context">The JsonSchemaGeneratorContext providing access to options and utilities.</param>
    /// <param name="type">The .NET Type to get a name for, guaranteed to be supported by this handler.</param>
    /// <returns>The name to use for the type in schema definitions and references.</returns>
    string GetTypeName(JsonSchemaGeneratorContext context, Type type);
}
