using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Compose.Integrations.UmbracoCms.Core;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal static class ComposeNodeTypeHelper
{
    private static readonly HashSet<Type> ComposeNodeTypes =
    [
        typeof(IApiContent),
        typeof(IApiMedia),
        typeof(ComposeNode),
    ];

    internal static bool IsComposeNodeType(Type type)
    {
        return ComposeNodeTypes.Any(composeType => composeType.IsAssignableFrom(type));
    }
}
