using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.Ingestion;
using Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Cache.Content;
using Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Migrations;
using Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Persistence;

namespace Umbraco.Cms.Core.DependencyInjection;

/// <summary>
/// Extension methods for Umbraco Compose Ingestion services.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    extension(IUmbracoBuilder builder)
    {
        /// <summary>
        /// Adds the Umbraco Compose ingestion services.
        /// </summary>
        /// <returns>The <see cref="IUmbracoBuilder"/>.</returns>
        public IUmbracoBuilder AddUmbracoComposeIngestion()
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddOptions<UmbracoComposeIngestionOptions>()
                .BindConfiguration("Umbraco:Compose:Ingestion");

            builder
                .AddComposeCustomCacheRefresherNotificationHandlers();

            builder.Services.AddSingleton<IIngestQueueRepository, IngestQueueRepository>();
            builder.AddComponent<IngestQueueMigrationComponent>();

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

            builder.AddComponent<IngestionQueueDrainComponent>();

            builder
                .AddNotificationAsyncHandler<
                    PublishedContentCacheRefresherNotification, PublishedContentCacheRefresherNotificationHandler>();

            return builder;
        }
    }
}
