namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

/// <summary>
/// Defines the contract for generating type names for JSON schema generation.
/// </summary>
public interface ITypeNameGenerator
{
    /// <summary>
    /// Generates a type name from the specified type.
    /// </summary>
    /// <param name="type">The type to generate a name for.</param>
    /// <returns>The generated type name.</returns>
    string GenerateName(Type type);
}
