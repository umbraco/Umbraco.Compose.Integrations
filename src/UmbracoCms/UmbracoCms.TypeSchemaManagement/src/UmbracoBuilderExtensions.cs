using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

namespace Umbraco.Cms.Core.DependencyInjection;

/// <summary>
/// Extension methods for Umbraco Compose Schema Management services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    extension(IUmbracoBuilder builder)
    {
        /// <summary>
        /// Adds the Umbraco Compose schema management services.
        /// </summary>
        public IUmbracoBuilder AddUmbracoComposeTypeSchemaManagement()
        {
            ArgumentNullException.ThrowIfNull(builder);

            _ = builder.Services.AddSingleton(Channel.CreateUnbounded<SchemaQueueItem>());
            _ = builder.Services.AddSingleton(static sp => sp.GetRequiredService<Channel<SchemaQueueItem>>().Writer);

            _ = builder.Services.AddScoped<IContentTypeSchemaService, ContentTypeSchemaService>();
            _ = builder.Services.AddScoped<JsonSchemaExporterService>();

            _ = builder.Services.AddHttpClient<ManagementApiService>()
                .ConfigureHttpClient(static (services, client) =>
                {
                    IOptions<UmbracoComposeOptions> options =
                        services.GetRequiredService<IOptions<UmbracoComposeOptions>>();
                    client.BaseAddress = options.Value.GetManagementUrl();
                })
                .AddUmbracoComposeAuthenticationMessageHandler()
                .SetProductInformation(typeof(SchemaBackgroundService).Assembly);

            _ = builder.Services.AddHostedService<SchemaBackgroundService>();

            _ = builder
                .AddNotificationAsyncHandler<
                    ContentTypeSavedNotification, ContentTypeNotificationHandler>();

            return builder;
        }
    }
}
