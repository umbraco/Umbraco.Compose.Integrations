using System.Reflection;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.Schema;

namespace Umbraco.Cms.Core.DependencyInjection;

public static partial class UmbracoBuilderExtensions
{
    extension(IUmbracoBuilder builder)
    {
        public IUmbracoBuilder AddUmbracoComposeSchemaForwarding()
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddSingleton(Channel.CreateUnbounded<SchemaQueueItem>());
            builder.Services.AddSingleton(sp => sp.GetRequiredService<Channel<SchemaQueueItem>>().Writer);

            builder.Services.AddScoped<IContentTypeSchemaService, ContentTypeSchemaService>();
            builder.Services.AddScoped<JsonSchemaExporterService>();

            builder.Services.AddHttpClient<ManagementApiService>()
                .ConfigureHttpClient(static (services, client) =>
                {
                    IOptions<UmbracoComposeOptions> options =
                        services.GetRequiredService<IOptions<UmbracoComposeOptions>>();
                    AssemblyName name = typeof(ManagementApiService).Assembly.GetName()!;
                    client.DefaultRequestHeaders.UserAgent.Add(new(name.Name!, name.Version!.ToString()));
                    client.BaseAddress = options.Value.GetManagementUrl();
                })
                .AddUmbracoComposeAuthenticationMessageHandler()
                .SetProductInformation(typeof(SchemaBackgroundService).Assembly);

            builder.Services.AddHostedService<SchemaBackgroundService>();

            builder
                .AddNotificationAsyncHandler<
                    ContentTypeSavedNotification, ContentTypeNotificationHandler>();

            return builder;
        }
    }
}