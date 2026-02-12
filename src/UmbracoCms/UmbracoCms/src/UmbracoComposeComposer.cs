using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Compose.Integrations.UmbracoCms;

/// <summary>
/// Adds Umbraco Compose integration to the Umbraco CMS.
/// </summary>
public sealed class UmbracoComposeComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder
            .AddUmbracoComposeAuthentication()
            .AddUmbracoComposeQueuePersistence()
            .AddUmbracoComposeIngestion()
            .AddUmbracoComposeDataSource()
            .AddUmbracoComposeTypeSchemaManagement();
    }
}
