namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Represents an upsert entry.
/// </summary>
/// <typeparam name="T">The entry data type.</typeparam>
public abstract class UpsertEntry<T> : IngestEntry
{
    /// <inheritdoc />
    public override string Action { get; } = "upsert";

    /// <summary>
    /// The entry type schema alias.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// The entry data to ingest.
    /// </summary>
    public required T Data { get; set; }
}
