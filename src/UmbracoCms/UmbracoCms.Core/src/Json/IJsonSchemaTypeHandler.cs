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
    /// <param name="type">The .NET Type to generate a schema for, guaranteed to be supported by this handler</param>
    /// <returns>A JsonSchema instance representing the type, or null if generation fails and the type should be skipped</returns>
    JsonSchema? Handle(Type type);
}
