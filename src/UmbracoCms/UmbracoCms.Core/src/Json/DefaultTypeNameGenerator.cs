namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

internal sealed class DefaultTypeNameGenerator : ITypeNameGenerator
{
    public static readonly DefaultTypeNameGenerator Instance = new();

    private DefaultTypeNameGenerator() { }

    public string GenerateName(Type type) =>
        type.Name;
}
