using Umbraco.Cms.Core.Packaging;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Migrations;

/// <summary>
/// Package migration plan for the TypeSchemaManagement package.
/// </summary>
public sealed class TypeSchemaMigrationPlan : PackageMigrationPlan
{
    /// <inheritdoc />
    public TypeSchemaMigrationPlan()
        : base("Umbraco Compose TypeSchema") { }

    /// <inheritdoc />
    protected override void DefinePlan()
    {
        To<AddSchemaQueueTableMigration>("a3e4c917-6d52-4f89-b1a4-82c5d9f07e3a");
    }
}
