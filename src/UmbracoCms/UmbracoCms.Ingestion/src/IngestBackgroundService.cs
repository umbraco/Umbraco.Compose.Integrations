using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    private readonly Channel<IngestQueueItem> _channel;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IngestBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IScopeProvider _scopeProvider;
    private readonly ConcurrentDictionary<Type, Func<IngestQueueItem, CancellationToken, IAsyncEnumerable<IngestEntry>>> _processors = [];

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
        try
        {
            await foreach (IngestQueueItem queueItem in _channel.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                await using AsyncServiceScope serviceScope = _serviceProvider.CreateAsyncScope();
                IIngestQueueRepository queueRepository = serviceScope.ServiceProvider.GetRequiredService<IIngestQueueRepository>();

                try
                {
                    IReadOnlyCollection<IngestEntry> entries = await ProcessItemAsync(
                        queueItem, serviceScope.ServiceProvider, stoppingToken)
                        .ConfigureAwait(false);

                    if (entries.Count is 0)
                    {
                        _logger.LogInformation("No items to ingest");
                        continue;
                    }

                    using HttpClient httpClient = _httpClientFactory.CreateClient(nameof(IngestBackgroundService));

                    using HttpResponseMessage response = await httpClient
                        .PutAsJsonAsync(
                            _ingestionOptions.GetCollectionAlias(queueItem.GetType()),
                            entries,
                            s_jsonSerializerOptions,
                            cancellationToken: stoppingToken)
                        .ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error ingesting content");
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
        catch (OperationCanceledException)
        {
            // ignore
        }
    }

    private async Task<IReadOnlyCollection<IngestEntry>> ProcessItemAsync(
        IngestQueueItem queueItem,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        Type type = queueItem.GetType();
        Func<IngestQueueItem, CancellationToken, IAsyncEnumerable<IngestEntry>> processor =
            _processors.GetOrAdd(type, type => CreateProcessor(type, services));

        using IScope scope = _scopeProvider.CreateScope();

        List<IngestEntry> entries = await processor(queueItem, cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        scope.Complete();

        return entries;
    }

    private void OnIngestionOptionsChange(UmbracoComposeIngestionOptions options, string? name)
    {
        _ingestionOptions = options;
    }

    private static Func<IngestQueueItem, CancellationToken, IAsyncEnumerable<IngestEntry>> CreateProcessor(
        Type type,
        IServiceProvider services)
    {
        Type processorType = typeof(IIngestQueueItemProcessor<>).MakeGenericType(type);
        object processor = services.GetRequiredService(processorType);

        ParameterExpression itemParameter = Expression.Parameter(typeof(IngestQueueItem), "item");
        ParameterExpression cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        MethodCallExpression callExpression = Expression.Call(
            Expression.Constant(processor),
            processorType.GetMethod(nameof(IIngestQueueItemProcessor<>.ProcessAsync))!,
            Expression.Convert(itemParameter, type),

            cancellationTokenParameter);

        return Expression.Lambda<Func<IngestQueueItem, CancellationToken, IAsyncEnumerable<IngestEntry>>>(
            callExpression,
            itemParameter,
            cancellationTokenParameter
        )
            .Compile();
    }
}
