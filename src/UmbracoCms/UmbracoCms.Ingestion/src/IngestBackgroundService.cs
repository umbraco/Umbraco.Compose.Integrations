using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Persistence;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal sealed class IngestBackgroundService : BackgroundService
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Channel<IngestQueueItem> _channel;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IngestBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IScopeProvider _scopeProvider;

    private UmbracoComposeIngestionOptions _ingestionOptions;

    public IngestBackgroundService(
        Channel<IngestQueueItem> channel,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<UmbracoComposeIngestionOptions> ingestionOptions,
        ILogger<IngestBackgroundService> logger,
        IServiceProvider serviceProvider,
        IScopeProvider scopeProvider)
    {
        ArgumentNullException.ThrowIfNull(channel);

        _channel = channel;
        _httpClientFactory = httpClientFactory;
        _ingestionOptions = ingestionOptions.CurrentValue;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _scopeProvider = scopeProvider;

        ingestionOptions.OnChange(OnIngestionOptionsChange);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IngestQueueItem queueItem = await _channel.Reader.ReadAsync(stoppingToken).ConfigureAwait(false);

            await using AsyncServiceScope serviceScope = _serviceProvider.CreateAsyncScope();
            IIngestQueueRepository queueRepository = serviceScope.ServiceProvider.GetRequiredService<IIngestQueueRepository>();

            try
            {
                // TODO: the background worker should find a processor for the payloadType and call the ProcessAsync method
                // which returns one or more entries that should be ingested
                if (queueItem is not ContentIngestQueueItem contentIngestQueueItem)
                {
                    _logger.LogError(
                        "Don't know how to process {QueueItem}, currently only 'ContentIngestQueueItem' is supported",
                        queueItem);
                    continue;
                }

                ContentIngestQueueItemProcessor processor = serviceScope.ServiceProvider
                    .GetRequiredService<ContentIngestQueueItemProcessor>();

                List<IngestEntry> items;
                using (IScope scope = _scopeProvider.CreateScope())
                {
                    items = await processor.ProcessAsync(contentIngestQueueItem, stoppingToken)
                        .ToListAsync(cancellationToken: stoppingToken)
                        .ConfigureAwait(false);

                    scope.Complete();
                }

                if (items.Count is 0)
                {
                    _logger.LogInformation("No items to ingest");
                    continue;
                }

                try
                {
                    using HttpClient httpClient = _httpClientFactory.CreateClient(nameof(IngestBackgroundService));

                    // TODO: Make collection configurable based on entity type, either through configuration or maybe through a service
                    // e.g. when we add support for media one probably want that to go into another collection.

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
            finally
            {
                try
                {
                    await queueRepository.DeleteByIdAsync(queueItem.Id, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete queue item {QueueItemId} from database", queueItem.Id);
                }
            }
        }
    }

    private void OnIngestionOptionsChange(UmbracoComposeIngestionOptions options, string? name)
    {
        _ingestionOptions = options;
    }
}
