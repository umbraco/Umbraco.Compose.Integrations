using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;
using Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;
using Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.JsonSchemaTypeHandlers;
using Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Persistence;

namespace Umbraco.Cms.Core.DependencyInjection;

/// <summary>
/// Extension methods for Umbraco Compose Schema Management services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Extension methods for adding Umbraco Compose Schema Management services to the Umbraco builder.
    /// </summary>
    extension(IUmbracoBuilder builder)
    {
        /// <summary>
        /// Adds the Umbraco Compose schema management services.
        /// </summary>
        /// <returns>The Umbraco builder with schema management services added.</returns>
        public IUmbracoBuilder AddUmbracoComposeTypeSchemaManagement()
        {
            ArgumentNullException.ThrowIfNull(builder);

            _ = builder.Services.AddSingleton<ISchemaQueueRepository, SchemaQueueRepository>();

            _ = builder.Services.AddSingleton(Channel.CreateUnbounded<SchemaQueueItem>());
            _ = builder.Services.AddSingleton(static sp => sp.GetRequiredService<Channel<SchemaQueueItem>>().Writer);

            builder.Services.AddScoped<JsonSchemaExporterService>();

            builder.Services.AddOptions<JsonOptions>(nameof(SchemaBackgroundService))
                .Configure(o =>
                    o.SerializerOptions.TypeInfoResolver = TypeSchemaJsonSerializerContext.Default
                );

            builder.Services.AddOptions<JsonSchemaGeneratorOptions>(nameof(JsonSchemaExporterService))
                .Configure(o =>
                {
                    o.DefaultSchema = "https://umbracocompose.com/v1/schema";
                    o.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    o.ReferenceMode = ReferenceMode.External;

                    o.Handlers.Add(new ApiElementHandler());
                    o.Handlers.Add(new ApiContentRouteHandler());
                    o.Handlers.Add(new ApiContentStartItemHandler());
                    o.Handlers.Add(new ApiMediaWithCropsHandler());
                });

            builder.Services.AddHttpClient<SchemaBackgroundService>()
                .ConfigureHttpClient(static (services, client) =>
                {
                    IOptions<UmbracoComposeOptions> options =
                        services.GetRequiredService<IOptions<UmbracoComposeOptions>>();
                    client.BaseAddress = options.Value.GetManagementUrl();
                })
                .AddUmbracoComposeAuthenticationMessageHandler()
                .SetProductInformation(typeof(SchemaBackgroundService).Assembly)
                .AddStandardResilienceHandler();

            builder.Services.AddHostedService<SchemaBackgroundService>();

            builder
                .AddNotificationAsyncHandler<
                    ContentTypeSavedNotification, ContentTypeNotificationHandler>()
                .AddNotificationAsyncHandler<
                    ContentTypeSavedNotification, ContentTypeNotificationHandler>();

            builder.AddComponent<RequeueOnStartupComponent>();

            return builder;
        }
    }
}
