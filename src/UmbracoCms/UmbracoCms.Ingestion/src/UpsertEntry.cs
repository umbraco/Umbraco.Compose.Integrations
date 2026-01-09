namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

public abstract class UpsertEntry<T> : IngestEntry
{
    public override string Action { get; } = "upsert";

    public required T Data { get; set; }
}
