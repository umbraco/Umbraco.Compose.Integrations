using Umbraco.Cms.Core.Composing;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core;

/// <summary>
/// Represents a builder for the <see cref="PropertySchemaResolverCollection"/>.
/// </summary>
public class PropertySchemaResolverCollectionBuilder :
    OrderedCollectionBuilderBase<PropertySchemaResolverCollectionBuilder, PropertySchemaResolverCollection, IPropertySchemaResolver>
{
    /// <inheritdoc />
    protected override PropertySchemaResolverCollectionBuilder This =>
        this;
}
