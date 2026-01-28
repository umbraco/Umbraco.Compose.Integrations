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

            builder.Services.AddOptions<UmbracoComposeIngestionOptions>()
                .BindConfiguration("Umbraco:Compose:Ingestion")
                .ValidateDataAnnotations()
                .ValidateOnStart();

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
                    client.BaseAddress = options.Value.GetIngestionUrl();
                })
                .AddUmbracoComposeAuthenticationMessageHandler()
                .SetProductInformation(typeof(IngestBackgroundService).Assembly);

            builder.Services.AddHostedService<IngestBackgroundService>();

            builder
                .AddNotificationAsyncHandler<
                    PublishedContentCacheRefresherNotification, PublishedContentCacheRefresherNotificationHandler>();

            return builder;
        }
    }
}
