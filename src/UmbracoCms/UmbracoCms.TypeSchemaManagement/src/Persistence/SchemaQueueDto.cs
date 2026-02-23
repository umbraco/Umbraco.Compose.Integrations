using NPoco;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Persistence;

/// <summary>Database DTO for a type schema queue item.</summary>
[TableName("umbracoComposeSchemaQueue")]
[PrimaryKey("id", AutoIncrement = false)]
public sealed class SchemaQueueDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the item was queued.</summary>
    [Column("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the content type alias this schema change applies to.</summary>
    [Column("contentTypeAlias")] public string ContentTypeAlias { get; set; } = default!;
}
