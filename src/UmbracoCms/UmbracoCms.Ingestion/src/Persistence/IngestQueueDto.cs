using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Persistence;

/// <summary>Database DTO for a generic ingest queue item with JSON payload.</summary>
[TableName("umbracoComposeIngestQueue")]
[PrimaryKey("id", AutoIncrement = false)]
public sealed class IngestQueueDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the item was queued.</summary>
    [Column("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the assembly-qualified type name of the queue item.</summary>
    [Column("itemType")]
    public string ItemType { get; set; } = default!;

    /// <summary>Gets or sets the JSON-serialized payload.</summary>
    [Column("payload")]
    [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
    public string Payload { get; set; } = default!;
}
