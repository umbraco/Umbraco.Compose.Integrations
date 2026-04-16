namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

/// <summary>
/// Provides abstraction for generating type names used in JSON schema definitions and references.
/// Type names are used as keys in $defs dictionaries and in fragment URIs like #/$defs/{name}.
/// Implement this abstract class to customize how .NET types are converted to schema-friendly
/// names, allowing control over naming conventions for generated schema components.
/// </summary>
public abstract class TypeNameGenerator
{
    /// <summary>
    /// Gets the default TypeNameGenerator instance that uses simple type.Name for name generation.
    /// This default instance is used when no custom TypeNameGenerator is specified in options and
    /// provides basic naming without special handling for generic types, nested types, or other
    /// complex type structures.
    /// </summary>
    public static TypeNameGenerator Default { get; } = new DefaultTypeNameGenerator();

    /// <summary>
    /// Generates a type name from the specified type. This method is called during schema generation
    /// to produce names for use in $defs dictionaries and reference URIs. Implementations should
    /// generate unique, valid identifier strings that can be safely used as JSON property names
    /// and URI fragments. The generated name becomes the key for referencing the schema.
    /// </summary>
    /// <param name="type">The type to generate a name for.</param>
    /// <returns>The generated type name.</returns>
    public abstract string GenerateName(Type type);
}
