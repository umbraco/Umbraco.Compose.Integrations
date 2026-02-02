using Umbraco.Cms.Core.Models.DeliveryApi;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal static class ComposeNodeTypeHelper
{
    private static readonly HashSet<Type> ComposeNodeTypes =
    [
        typeof(IApiContent),
        typeof(IApiMedia),
    ];

    internal static bool IsComposeNodeType(Type type)
    {
        return ComposeNodeTypes.Any(composeType => composeType.IsAssignableFrom(type));
    }
}
