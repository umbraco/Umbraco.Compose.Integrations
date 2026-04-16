namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

internal sealed class DefaultTypeNameGenerator : TypeNameGenerator
{
    public override string GenerateName(Type type) =>
        type.Name;
}
