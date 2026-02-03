using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Umbraco.Compose.Integrations.UmbracoCms.DataSource;

internal sealed class UmbracoComposeSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc(
            Constants.DataSourceApiName,
            new OpenApiInfo { Title = Constants.DataSourceApiTitle, Version = Constants.Version_1 }
        );

        options.OperationFilter<UmbracoComposeOperationSecurityFilter>();
    }
}
