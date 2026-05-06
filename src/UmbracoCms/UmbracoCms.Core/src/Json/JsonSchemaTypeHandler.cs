namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

/// <summary>
/// Provides a generic base implementation for custom JSON schema type handlers. This abstract class
/// implements the CanHandle method to automatically match against the generic type T, reducing boilerplate
/// code for single-type handlers. Implement this class when you need to create handlers for specific
/// .NET types without needing to manually implement type matching logic. The derived class only needs
/// to provide implementations for GetTypeName and Handle methods.
/// </summary>
/// <typeparam name="T">The .NET type that this handler specializes in generating schemas for.</typeparam>
public abstract class JsonSchemaTypeHandler<T> : IJsonSchemaTypeHandler
{
    /// <inheritdoc />
    public bool CanHandle(Type type) =>
        type == typeof(T);

    /// <inheritdoc />
    public abstract string GetTypeName(JsonSchemaGeneratorContext context, Type type);

    /// <inheritdoc />
    public abstract JsonSchema Handle(JsonSchemaGeneratorContext context, Type type);
}
