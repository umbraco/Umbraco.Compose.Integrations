using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Client;
using Polly;
using Polly.Extensions.Http;
using Umbraco.Compose.Integrations.UmbracoCms.Core;

namespace Umbraco.Cms.Core.DependencyInjection;

public static class Extensions
{
    extension(IUmbracoBuilder builder)
    {
        public IUmbracoBuilder AddUmbracoComposeAuthentication()
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddOptions<UmbracoComposeOptions>()
                .BindConfiguration("Umbraco:Compose")
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddTransient<AuthenticationHttpMessageHandler>();

            // TODO: these should not be registered as default as it'll override / interfere with other registrations of OpenIddict client
            // can we register as keyed services or maybe use a child container?
            // we should probably also configure token cache (either in memory? or using efcore), and add dataprotection
            builder.Services
                .AddOpenIddict()
                .AddClient(options =>
                {
                    options.AllowClientCredentialsFlow();

                    options.UseSystemNetHttp(options => options
                        .SetProductInformation(typeof(Extensions).Assembly)
                        .SetHttpErrorPolicy(HttpPolicyExtensions.HandleTransientHttpError()
                            .OrResult(static response => response.StatusCode is System.Net.HttpStatusCode.NotFound)
                            .WaitAndRetryAsync(5, static attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
                    );
                });

            builder.Services.AddOptions<OpenIddictClientOptions>()
                .Configure<IOptions<UmbracoComposeOptions>>(static (options, composeOptions) =>
                {
                    options.Registrations.Add(new()
                    {
                        Issuer = composeOptions.Value.GetManagementBaseUrl(),
                        ClientId = composeOptions.Value.ClientId,
                        ClientSecret = composeOptions.Value.ClientSecret,
                    });
                });

            return builder;
        }
    }

    extension(IHttpClientBuilder builder)
    {
        public IHttpClientBuilder AddUmbracoComposeAuthenticationMessageHandler()
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder
                .AddHttpMessageHandler(static services => services.GetRequiredService<AuthenticationHttpMessageHandler>());
        }
    }
}
