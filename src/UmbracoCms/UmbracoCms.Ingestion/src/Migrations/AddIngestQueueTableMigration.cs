using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Persistence;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Migrations;

/// <summary>
/// Creates the ingest queue persistence database table and index.
/// </summary>
/// <param name="context">The migration context.</param>
public sealed class AddIngestQueueTableMigration(IMigrationContext context) : AsyncMigrationBase(context)
{
    /// <inheritdoc />
    protected override Task MigrateAsync()
    {
        if (!TableExists("umbracoComposeIngestQueue"))
        {
            Create.Table<IngestQueueDto>().Do();
            Create.Index($"IX_umbracoComposeIngestQueue_{nameof(IngestQueueDto.CreatedAt)}")
                .OnTable("umbracoComposeIngestQueue")
                .OnColumn(nameof(IngestQueueDto.CreatedAt))
                .Ascending()
                .WithOptions()
                .NonClustered()
                .Do();
        }

        return Task.CompletedTask;
    }
}
