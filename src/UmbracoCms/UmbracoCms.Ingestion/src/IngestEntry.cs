using System.Text.Json.Serialization;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Base class for the different ingest actions.
/// </summary>
[JsonConverter(typeof(IngestEntryJsonConverter))]
public abstract class IngestEntry
{
    /// <summary>
    /// The action to perform.
    /// </summary>
    public abstract string Action { get; }

    /// <summary>
    /// The id of the entry.
    /// </summary>
    public virtual string? Id { get; set; }

    ///<summary>
    /// The variant of the entry.
    /// </summary>
    public string? Variant { get; set; }
}
