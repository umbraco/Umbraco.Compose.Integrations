using Umbraco.Cms.Core.Packaging;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Migrations;

/// <summary>
/// Package migration plan for schema queue persistence.
/// </summary>
public sealed class SchemaQueueMigrationPlan : PackageMigrationPlan
{
    /// <inheritdoc />
    public SchemaQueueMigrationPlan()
        : base("UmbracoComposeSchemaQueuePersistence") { }

    /// <inheritdoc />
    protected override void DefinePlan()
    {
        To<AddSchemaQueueTableMigration>("a3e4c917-6d52-4f89-b1a4-82c5d9f07e3a");
    }
}
