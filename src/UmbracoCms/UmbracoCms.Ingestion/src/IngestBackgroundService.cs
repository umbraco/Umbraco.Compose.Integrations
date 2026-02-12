using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence;
using Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence.Repositories;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal sealed class IngestBackgroundService : BackgroundService
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly Channel<IngestQueueItem> _channel;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IngestBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    private UmbracoComposeIngestionOptions _ingestionOptions;

    public IngestBackgroundService(
        Channel<IngestQueueItem> channel,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<UmbracoComposeIngestionOptions> ingestionOptions,
        ILogger<IngestBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(channel);

        _channel = channel;
        _httpClientFactory = httpClientFactory;
        _ingestionOptions = ingestionOptions.CurrentValue;
        _logger = logger;
        _serviceProvider = serviceProvider;

        ingestionOptions.OnChange(OnIngestionOptionsChange);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IngestQueueItem queueItem = await _channel.Reader.ReadAsync(stoppingToken).ConfigureAwait(false);

            await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
            IContentQueueRepository queueRepository = scope.ServiceProvider.GetRequiredService<IContentQueueRepository>();
            try
            {
                await queueRepository.DeleteByQueueItemIdAsync(queueItem.Id, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete queue item {QueueItemId} from database", queueItem.Id);
            }

            // TODO: the background worker should find a processor for the payloadType and call the ProcessAsync method
            // which returns one or more entries that should be ingested
            // when done the db entry should be deleted (or marked as complete)

            if (queueItem is not ContentIngestQueueItem contentIngestQueueItem)
            {
                _logger.LogDebug("Don't know how to process {QueueItem}, currently only 'ContentIngestQueueItem' is supported", queueItem);
                continue;
            }

            UmbracoContentIngestItemQueueProcessor processor = scope.ServiceProvider
                .GetRequiredService<UmbracoContentIngestItemQueueProcessor>();

            List<IngestEntry> items = await processor.ProcessAsync(contentIngestQueueItem, stoppingToken)
                .ToListAsync(cancellationToken: stoppingToken)
                .ConfigureAwait(false);

            if (items.Count is 0)
            {
                _logger.LogInformation("No items to ingest");
                continue;
            }

            try
            {
                using HttpClient httpClient = _httpClientFactory.CreateClient(nameof(IngestBackgroundService));

                // TODO: Make collection configurable based on type, either through configuration or maybe through a service
                // e.g. you might want to send a specific content type to a specific collection, in first iteration we could
                // just do a configuration option where you set it in configuration for the entity type (content, media)
                using HttpResponseMessage response = await httpClient
                    .PutAsJsonAsync(
                        _ingestionOptions.CollectionAlias,
                        items,
                        s_jsonSerializerOptions,
                        cancellationToken: stoppingToken)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ingesting content");
            }
        }
    }

    private void OnIngestionOptionsChange(UmbracoComposeIngestionOptions options, string? name)
    {
        _ingestionOptions = options;
    }
}
