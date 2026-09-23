using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Persistence;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Migrations;

/// <summary>
/// Creates the schema queue persistence database table and index.
/// </summary>
/// <param name="context">The migration context.</param>
internal sealed class AddSchemaQueueTableMigration(IMigrationContext context) : MigrationBase(context)
{
    /// <inheritdoc />
    protected override void Migrate()
    {
        if (TableExists("umbracoComposeSchemaQueue"))
        {
            return;
        }

        Create.Table<SchemaQueueDto>().Do();
        Create.Index($"IX_umbracoComposeSchemaQueue_{nameof(SchemaQueueDto.CreatedAt)}")
            .OnTable("umbracoComposeSchemaQueue")
            .OnColumn(nameof(SchemaQueueDto.CreatedAt))
            .Ascending()
            .WithOptions()
            .NonClustered()
            .Do();
    }
}
