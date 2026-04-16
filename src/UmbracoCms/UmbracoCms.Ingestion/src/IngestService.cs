using System.Text.Json;
using System.Threading.Channels;
using Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Persistence;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal sealed class IngestService : IIngestService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ChannelWriter<IngestQueueItem> _writer;
    private readonly IIngestQueueRepository _queueRepository;

    public IngestService(
        Channel<IngestQueueItem> channel,
        IIngestQueueRepository queueRepository)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(queueRepository);
        _queueRepository = queueRepository;

        _writer = channel.Writer;
    }

    public async ValueTask EnqueueAsync(IngestQueueItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        IngestQueueDto dto = new()
        {
            Id = item.Id,
            CreatedAt = item.CreatedAt,
            ItemType = $"{item.GetType().FullName}, {item.GetType().Assembly.GetName().Name}",
            Payload = JsonSerializer.Serialize(item, item.GetType(), s_jsonOptions)
        };

        await _queueRepository.InsertAsync(dto, cancellationToken).ConfigureAwait(false);

        await _writer.WriteAsync(item, cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask EnqueueAsync(IEnumerable<IngestQueueItem> items, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (IngestQueueItem item in items)
        {
            await EnqueueAsync(item, cancellationToken).ConfigureAwait(false);
        }
    }
}
