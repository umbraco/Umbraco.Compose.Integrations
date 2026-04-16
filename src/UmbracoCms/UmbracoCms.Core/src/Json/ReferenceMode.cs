namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

/// <summary>
/// Represents the reference handling modes for schema generation when converting .NET types
/// to JSON Schema. Each mode determines how referenced types are handled in the output schema,
/// affecting schema size, reusability, and cross-referencing capabilities.
/// </summary>
public enum ReferenceMode
{
    /// <summary>
    /// Uses $defs to store reusable type definitions and references them using fragment URIs
    /// like #/defs/TypeName. This mode produces self-contained schemas with inline definitions
    /// that can be validated independently while avoiding duplication of complex nested structures.
    /// </summary>
    Defs = 0,

    /// <summary>
    /// Inlines all schema definitions directly without using references. This mode produces
    /// larger but fully self-describing schemas that don't depend on external resolution or
    /// fragment navigation. Useful for simple schemas or when reference resolution is problematic.
    /// </summary>
    Inline = 1,

    /// <summary>
    /// Uses external references by type name URI without $defs. This mode generates schemas
    /// intended to be split across multiple files, with each type referenced by its URI.
    /// Requires a schema registry or directory structure to resolve external references.
    /// </summary>
    External = 2
}
