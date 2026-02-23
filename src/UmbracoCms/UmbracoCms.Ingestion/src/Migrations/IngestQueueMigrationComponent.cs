using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Infrastructure.Scoping;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Migrations;

/// <summary>
/// Umbraco component that runs ingest queue persistence database migrations on startup.
/// </summary>
/// <param name="scopeProvider">The scope provider for database access.</param>
/// <param name="migrationPlanExecutor">The migration plan executor.</param>
/// <param name="keyValueService">The key-value service for migration state.</param>
/// <param name="runtimeState">The Umbraco runtime state.</param>
public sealed class IngestQueueMigrationComponent(
    IScopeProvider scopeProvider,
    IMigrationPlanExecutor migrationPlanExecutor,
    IKeyValueService keyValueService,
    IRuntimeState runtimeState) : IAsyncComponent
{
    /// <inheritdoc />
    public async Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        if (runtimeState.Level < RuntimeLevel.Run)
        {
            return;
        }

        MigrationPlan migrationPlan = new("UmbracoComposeIngestQueuePersistence");
        migrationPlan.From(string.Empty)
            .To<AddIngestQueueTableMigration>("compose-ingest-queue-table-1.0.0");

        Upgrader upgrader = new(migrationPlan);
        await upgrader.ExecuteAsync(migrationPlanExecutor, scopeProvider, keyValueService)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
