using System.Reflection;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.Ingestion;
using Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Cache.Content;

namespace Umbraco.Cms.Core.DependencyInjection;

public static partial class UmbracoBuilderExtensions
{
    extension(IUmbracoBuilder builder)
    {
        public IUmbracoBuilder AddUmbracoComposeIngestion()
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder
                .AddUmbracoComposeAuthentication()
                .AddComposeCustomCacheRefresherNotificationHandlers();

            builder.Services.AddSingleton(static _ => Channel.CreateUnbounded<IngestQueueItem>());
            builder.Services.AddSingleton<IIngestService, IngestService>()
                .AddScoped<UmbracoContentIngestItemQueueProcessor>();

            builder.Services.AddHttpClient<IngestBackgroundService>()
                .ConfigureHttpClient(static (services, client) =>
                {
                    IOptions<UmbracoComposeOptions> options = services.GetRequiredService<IOptions<UmbracoComposeOptions>>();
                    AssemblyName name = typeof(IngestBackgroundService).Assembly.GetName()!;
                    client.DefaultRequestHeaders.UserAgent.Add(new(name.Name!, name.Version!.ToString()));
                    client.BaseAddress = options.Value.GetIngestionUrl();
                })
                .AddUmbracoComposeAuthenticationMessageHandler();

            builder.Services.AddHostedService<IngestBackgroundService>();

            builder
                .AddNotificationAsyncHandler<
                    PublishedContentCacheRefresherNotification, PublishedContentCacheRefresherNotificationHandler>();

            return builder;
        }
    }
}
