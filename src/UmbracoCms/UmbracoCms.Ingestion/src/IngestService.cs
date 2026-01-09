using System.Threading.Channels;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal sealed class IngestService : IIngestService
{
    private readonly ChannelWriter<IngestQueueItem> _writer;

    public IngestService(Channel<IngestQueueItem> channel)
    {
        ArgumentNullException.ThrowIfNull(channel);

        _writer = channel.Writer;
    }

    public ValueTask EnqueueAsync(IngestQueueItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        // TODO: this should also be persisted in the database along with the concrete type
        return _writer.WriteAsync(item, cancellationToken);
    }

    public async ValueTask EnqueueAsync(IEnumerable<IngestQueueItem> items, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (IngestQueueItem item in items)
        {
            await _writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
        }
    }
}
