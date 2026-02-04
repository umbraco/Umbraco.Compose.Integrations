using Umbraco.Cms.Api.Management.OpenApi;

namespace Umbraco.Compose.Integrations.UmbracoCms.DataSource;

internal sealed class UmbracoComposeOperationSecurityFilter : BackOfficeSecurityRequirementsOperationFilterBase
{
    protected override string ApiName =>
        Constants.DataSourceApiName;
}
