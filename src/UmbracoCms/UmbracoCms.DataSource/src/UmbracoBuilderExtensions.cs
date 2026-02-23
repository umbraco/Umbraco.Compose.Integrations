using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.DataSource;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Umbraco.Cms.Core.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Extension methods for adding Umbraco Compose Data Source services to the Umbraco builder.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds all required services for the Umbraco Compose Data Source.
    /// </summary>
    /// <returns>The Umbraco builder with data source services added.</returns>
    /// <remarks>
    /// This requires a call to <see cref="Extensions.AddUmbracoComposeAuthentication"/> in order to configure
    /// the <see cref="UmbracoComposeOptions"/> required to authorize requests to the Compose API.
    /// </remarks>
    extension(IUmbracoBuilder builder)
    {
        /// <summary>
        /// Adds all required services for the Umbraco Compose Data Source.
        /// </summary>
        /// <returns>The Umbraco builder with data source services added.</returns>
        /// <remarks>
        /// This requires a call to <see cref="Extensions.AddUmbracoComposeAuthentication"/> in order to configure
        /// the <see cref="UmbracoComposeOptions"/> required to authorize requests to the Compose API.
        /// </remarks>
        public IUmbracoBuilder AddUmbracoComposeDataSource()
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services
                .ConfigureOptions<UmbracoComposeSwaggerGenOptions>()
                .AddSingleton<IGraphQlContentQueryService, GraphQLContentQueryService>()
                .AddHttpClient<GraphQLContentQueryService>()
                .ConfigureHttpClient(static (services, client) =>
                {
                    IOptions<UmbracoComposeOptions> options = services.GetRequiredService<IOptions<UmbracoComposeOptions>>();
                    client.BaseAddress = options.Value.GetGraphQLUrl();
                    client.DefaultRequestHeaders.Add("GraphQL-Require-Preflight", "true");
                    client.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/graphql-response+json"));
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                })
                .AddUmbracoComposeAuthenticationMessageHandler()
                .SetProductInformation(typeof(UmbracoComposeDataSourceController).Assembly);

            return builder;
        }
    }
}
