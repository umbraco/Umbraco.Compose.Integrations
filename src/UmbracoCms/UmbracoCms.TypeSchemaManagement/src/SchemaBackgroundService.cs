using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Persistence;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal sealed class SchemaBackgroundService(
    Channel<SchemaQueueItem> channel,
    IServiceProvider serviceProvider,
    ILogger<SchemaBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            SchemaQueueItem queueItem = await channel.Reader.ReadAsync(stoppingToken).ConfigureAwait(false);

            await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

            ISchemaQueueRepository queueRepository = scope.ServiceProvider
                .GetRequiredService<ISchemaQueueRepository>();

            try
            {
                JsonSchemaExporterService schemaExporter = scope.ServiceProvider.GetRequiredService<JsonSchemaExporterService>();
                ManagementApiService apiService = scope.ServiceProvider.GetRequiredService<ManagementApiService>();

                JsonElement? jsonSchema = schemaExporter.GenerateSchema(queueItem.ContentTypeAlias);
                if (jsonSchema is null)
                {
                    continue;
                }

                TypeSchemaDto? existing = await apiService.GetTypeSchemaAsync(queueItem.ContentTypeAlias, stoppingToken)
                    .ConfigureAwait(false);

                if (existing is null)
                {
                    CreateTypeSchemaRequest request = new(
                        queueItem.ContentTypeAlias,
                        $"Created by Umbraco CMS: {queueItem.ContentTypeAlias}",
                        jsonSchema.Value);

                    _ = await apiService.CreateTypeSchemaAsync(request, stoppingToken).ConfigureAwait(false);

                    logger.LogInformation("Created type schema '{Alias}' in Umbraco Compose", queueItem.ContentTypeAlias);
                }
                else
                {
                    _ = await apiService.UpdateTypeSchemaSchemaAsync(
                        queueItem.ContentTypeAlias,
                        jsonSchema.Value,
                        stoppingToken)
                        .ConfigureAwait(false);

                    logger.LogInformation("Updated type schema '{Alias}' in Umbraco Compose", queueItem.ContentTypeAlias);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create or update type schema '{Alias}' in Umbraco Compose", queueItem.ContentTypeAlias);
            }
            finally
            {
                try
                {
                    await queueRepository.DeleteByIdAsync(queueItem.Id, stoppingToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete schema queue item {QueueItemId} from database", queueItem.Id);
                }
            }
        }
    }
}
