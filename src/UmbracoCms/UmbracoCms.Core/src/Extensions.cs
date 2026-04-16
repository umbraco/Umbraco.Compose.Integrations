using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Client;
using Polly;
using Polly.Extensions.Http;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using static OpenIddict.Client.OpenIddictClientEvents;
using static OpenIddict.Client.SystemNetHttp.OpenIddictClientSystemNetHttpHandlers;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Umbraco.Cms.Core.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Extension methods for the core Umbraco Compose services.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds the Umbraco Compose authentication services to the Umbraco builder.
    /// </summary>
    /// <returns>The Umbraco builder with authentication services added.</returns>
    extension(IUmbracoBuilder builder)
    {
        /// <summary>
        /// Adds the Umbraco Compose authentication services.
        /// </summary>
        /// <returns>The Umbraco builder.</returns>
        public IUmbracoBuilder AddUmbracoComposeAuthentication()
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddOptions<UmbracoComposeOptions>()
                .BindConfiguration("Umbraco:Compose");

            builder.Services.AddTransient<AuthenticationHttpMessageHandler>();

            builder.Services
                .AddOpenIddict()

                .AddClient(static options =>
                {
                    options.AllowClientCredentialsFlow()
                        // custom handler for setting user agent, this is for the configuration, jwt and token exchange endpoints
                        .AddEventHandler(OpenIddictClientHandlerDescriptor.CreateBuilder<PrepareConfigurationRequestContext>()
                            .UseSingletonHandler<SetUserAgentHeader<PrepareConfigurationRequestContext>>()
                            .SetOrder(AttachUserAgentHeader<PrepareConfigurationRequestContext>.Descriptor.Order - 1)
                            .SetType(OpenIddictClientHandlerType.Custom)
                            .Build())
                        .AddEventHandler(OpenIddictClientHandlerDescriptor.CreateBuilder<PrepareJsonWebKeySetRequestContext>()
                            .UseSingletonHandler<SetUserAgentHeader<PrepareJsonWebKeySetRequestContext>>()
                            .SetOrder(AttachUserAgentHeader<PrepareJsonWebKeySetRequestContext>.Descriptor.Order - 1)
                            .SetType(OpenIddictClientHandlerType.Custom)
                            .Build())
                        .AddEventHandler(OpenIddictClientHandlerDescriptor.CreateBuilder<PrepareTokenRequestContext>()
                            .UseSingletonHandler<SetUserAgentHeader<PrepareTokenRequestContext>>()
                            .SetOrder(AttachUserAgentHeader<PrepareTokenRequestContext>.Descriptor.Order - 1)
                            .SetType(OpenIddictClientHandlerType.Custom)
                            .Build());

                    options.UseSystemNetHttp(static options =>
                        options
                            .SetHttpErrorPolicy(HttpPolicyExtensions.HandleTransientHttpError()
                                .OrResult(static response => response.StatusCode is System.Net.HttpStatusCode.NotFound)
                                .WaitAndRetryAsync(5, static attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
                    );
                });

            builder.Services.AddOptions<OpenIddictClientOptions>()
                .Configure<IOptions<UmbracoComposeOptions>>(static (options, composeOptions) =>
                {
                    if (!composeOptions.Value.IsValid)
                    {
                        return;
                    }

                    options.Registrations.Add(new()
                    {
                        Issuer = composeOptions.Value.GetManagementBaseUrl(),
                        ClientId = composeOptions.Value.ClientId,
                        ClientSecret = composeOptions.Value.ClientSecret,
                        Properties = {
                            { "Umbraco-Compose-Integration-Core-Version", GetProductInformationHeaderValue(typeof(Extensions).Assembly)}
                        }
                    });
                });

            builder.WithCollectionBuilder<PropertySchemaResolverCollectionBuilder>()
                .Append(builder.TypeLoader.GetTypes<IPropertySchemaResolver>());

            return builder;
        }
    }

    /// <summary>
    /// Extension methods for <see cref="HttpClient"/>.
    /// </summary>
    extension(HttpClient client)
    {
        /// <summary>
        /// Sets the product information header on the HTTP client.
        /// </summary>
        /// <param name="assembly">The assembly to get the name and version from.</param>
        /// <returns>The HTTP client with the product information header set.</returns>
        public HttpClient SetProductInformation(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(assembly);

            ProductInfoHeaderValue? productInfo = GetProductInformationHeaderValue(assembly);

            if (productInfo is null)
            {
                return client;
            }

            client.DefaultRequestHeaders.UserAgent.Add(productInfo);

            return client;
        }
    }

    /// <summary>
    /// Extension methods for <see cref="IHttpClientBuilder"/>.
    /// </summary>
    extension(IHttpClientBuilder builder)
    {
        /// <summary>
        /// Adds the Umbraco Compose authentication message handler to the HTTP client builder.
        /// </summary>
        /// <returns>The HTTP client builder with the authentication message handler added.</returns>
        public IHttpClientBuilder AddUmbracoComposeAuthenticationMessageHandler()
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder
                .AddHttpMessageHandler(static services => services.GetRequiredService<AuthenticationHttpMessageHandler>());
        }

        /// <summary>
        /// Sets the product information header on the HTTP client configured by the builder.
        /// </summary>
        /// <param name="assembly">The assembly to get the name and version from.</param>
        /// <returns>The HTTP client builder with the product information header set.</returns>
        public IHttpClientBuilder SetProductInformation(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(assembly);

            builder.ConfigureHttpClient(client => client.SetProductInformation(assembly));

            return builder;
        }
    }

    /// <summary>
    /// Gets the product information header value from the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to get the product information from.</param>
    /// <returns>The product information header value, or null if the assembly name cannot be determined.</returns>
    private static ProductInfoHeaderValue? GetProductInformationHeaderValue(Assembly assembly)
    {
        AssemblyName name = assembly.GetName();
        if (name is null || name.Name is null)
        {
            return null;
        }

        return name.Version is null ?
            new(name.Name) :
            new(name.Name, name.Version.ToString());
    }
}
