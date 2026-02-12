using NPoco;
using Umbraco.Cms.Core.Services.Changes;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence;

/// <summary>Database DTO for a content queue payload entry.</summary>
[TableName("umbracoComposeContentQueuePayload")]
[PrimaryKey("id", AutoIncrement = false)]
public sealed class ContentQueuePayloadDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    [Column("id")] public Guid Id { get; set; }

    /// <summary>Gets or sets the parent queue item identifier.</summary>
    [Column("queueItemId")] public Guid QueueItemId { get; set; }

    /// <summary>Gets or sets the content node identifier.</summary>
    [Column("contentId")] public Guid ContentId { get; set; }

    /// <summary>Gets or sets the raw change type flags.</summary>
    [Column("changeTypes")] public byte ChangeTypes { get; set; }

    /// <summary>Gets or sets the comma-separated affected culture codes, or <c>null</c> for invariant content.</summary>
    [Column("affectedCultures")]
    [NullSetting(NullSetting = NullSettings.Null)]
    public string? AffectedCultures { get; set; }

    /// <summary>Gets or sets the tree change type flags.</summary>
    [Ignore]
    public TreeChangeTypes TreeChangeTypes
    {
        get => (TreeChangeTypes)ChangeTypes;
        set => ChangeTypes = (byte)value;
    }
}
