using System.Text.Json.Serialization;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

[JsonConverter(typeof(IngestEntryJsonConverter))]
public abstract class IngestEntry
{
    public abstract string Action { get; }
    public required string Id { get; set; }
    public string? Variant { get; set; }
}
