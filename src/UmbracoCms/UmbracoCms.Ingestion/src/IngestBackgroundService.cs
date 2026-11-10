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
    private readonly ConcurrentDictionary<Type, Func<object, IngestQueueItem, CancellationToken, IAsyncEnumerable<IngestEntry>>> _processors = [];
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
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
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
        Type processorType = typeof(IIngestQueueItemProcessor<>).MakeGenericType(type);

        Func<object, IngestQueueItem, CancellationToken, IAsyncEnumerable<IngestEntry>> processorDelegate =
            _processors.GetOrAdd(type, type => CreateProcessor(type));

        object processor = services.GetRequiredService(processorType);

        using IScope scope = _scopeProvider.CreateScope();

        List<IngestEntry> entries = await processorDelegate(processor, queueItem, cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        scope.Complete();

        return entries;
    }

    private void OnIngestionOptionsChange(UmbracoComposeIngestionOptions options, string? name)
    {
        _ingestionOptions = options;
    }

    private static Func<object, IngestQueueItem, CancellationToken, IAsyncEnumerable<IngestEntry>> CreateProcessor(Type type)
    {
        Type processorType = typeof(IIngestQueueItemProcessor<>).MakeGenericType(type);

        ParameterExpression processorParameter = Expression.Parameter(typeof(object), "processor");
        ParameterExpression itemParameter = Expression.Parameter(typeof(IngestQueueItem), "item");
        ParameterExpression cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        MethodCallExpression callExpression = Expression.Call(
            Expression.Convert(processorParameter, processorType),
            processorType.GetMethod(nameof(IIngestQueueItemProcessor<>.ProcessAsync))!,
            Expression.Convert(itemParameter, type),

            cancellationTokenParameter);

        return Expression.Lambda<Func<object, IngestQueueItem, CancellationToken, IAsyncEnumerable<IngestEntry>>>(
            callExpression,
            processorParameter,
            itemParameter,
            cancellationTokenParameter
        )
            .Compile();
    }
}
