using Umbraco.Cms.Core.Composing;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core;

/// <summary>
/// Represents a collection of <see cref="IPropertySchemaResolver"/> instances.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PropertySchemaResolverCollection"/> class.
/// </remarks>
/// <param name="items">A factory function that returns the collection items.</param>
public sealed class PropertySchemaResolverCollection(
    Func<IEnumerable<IPropertySchemaResolver>> items) : BuilderCollectionBase<IPropertySchemaResolver>(items)
{
}
