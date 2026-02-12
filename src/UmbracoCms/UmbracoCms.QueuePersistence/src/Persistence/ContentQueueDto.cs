using NPoco;

namespace Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence;

/// <summary>Database DTO for a content ingestion queue item.</summary>
[TableName("umbracoComposeContentQueue")]
[PrimaryKey("id", AutoIncrement = false)]
public sealed class ContentQueueDto : IHaveCreatedAt
{
    /// <summary>Gets or sets the unique identifier.</summary>
    [Column("id")] public Guid Id { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the item was queued.</summary>
    [Column("createdAt")] public DateTime CreatedAt { get; set; }
}
