using Umbraco.Cms.Core.Packaging;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Migrations;

/// <summary>
/// Package migration plan for ingest queue persistence.
/// </summary>
public sealed class IngestQueueMigrationPlan : PackageMigrationPlan
{
    /// <inheritdoc />
    public IngestQueueMigrationPlan()
        : base("UmbracoComposeIngestQueuePersistence") { }

    /// <inheritdoc />
    protected override void DefinePlan()
    {
        To<AddIngestQueueTableMigration>("b710cf36-3e13-43c8-9547-4a71cfdd5f8b");
    }
}
