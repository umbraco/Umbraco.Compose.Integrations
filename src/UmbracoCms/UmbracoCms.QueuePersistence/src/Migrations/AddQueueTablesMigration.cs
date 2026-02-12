using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence;

namespace Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Migrations;

/// <summary>
/// Creates the queue persistence database tables and indexes.
/// </summary>
/// <param name="context">The migration context.</param>
public sealed class AddQueueTablesMigration(IMigrationContext context) : AsyncMigrationBase(context)
{
    /// <inheritdoc />
    protected override Task MigrateAsync()
    {
        if (!TableExists("umbracoComposeContentQueue"))
        {
            Create.Table<ContentQueueDto>().Do();
            Create.Index($"IX_umbracoComposeContentQueue_{nameof(ContentQueueDto.CreatedAt)}")
                .OnTable("umbracoComposeContentQueue")
                .OnColumn(nameof(ContentQueueDto.CreatedAt))
                .Ascending()
                .WithOptions()
                .NonClustered()
                .Do();
        }

        if (!TableExists("umbracoComposeContentQueuePayload"))
        {
            Create.Table<ContentQueuePayloadDto>().Do();
            Create.Index($"IX_umbracoComposeContentQueuePayload_{nameof(ContentQueuePayloadDto.QueueItemId)}")
                .OnTable("umbracoComposeContentQueuePayload")
                .OnColumn(nameof(ContentQueuePayloadDto.QueueItemId))
                .Ascending()
                .WithOptions()
                .NonClustered()
                .Do();
        }

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
