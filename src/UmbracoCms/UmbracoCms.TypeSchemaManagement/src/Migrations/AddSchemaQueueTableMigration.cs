using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Persistence;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Migrations;

/// <summary>
/// Creates the schema queue persistence database table and index.
/// </summary>
/// <param name="context">The migration context.</param>
public sealed class AddSchemaQueueTableMigration(IMigrationContext context) : AsyncMigrationBase(context)
{
    /// <inheritdoc />
    protected override Task MigrateAsync()
    {
        if (!TableExists("umbracoComposeSchemaQueue"))
        {
            Create.Table<SchemaQueueDto>().Do();
            Create.Index($"IX_umbracoComposeSchemaQueue_{nameof(SchemaQueueDto.CreatedAt)}")
                .OnTable("umbracoComposeSchemaQueue")
                .OnColumn(nameof(SchemaQueueDto.CreatedAt))
                .Ascending()
                .WithOptions()
                .NonClustered()
                .Do();
        }

        return Task.CompletedTask;
    }
}
