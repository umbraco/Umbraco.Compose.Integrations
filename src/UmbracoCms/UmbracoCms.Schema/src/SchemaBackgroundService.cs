using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Umbraco.Compose.Integrations.UmbracoCms.Schema;

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

            try
            {
                await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

                var schemaExporter = scope.ServiceProvider.GetRequiredService<JsonSchemaExporterService>();
                var apiService = scope.ServiceProvider.GetRequiredService<ManagementApiService>();

                var jsonSchema = schemaExporter.GenerateSchema(queueItem.ContentTypeAlias);

                var existing = await apiService.GetTypeSchemaAsync(queueItem.ContentTypeAlias, stoppingToken);

                if (existing is null)
                {
                    var request = new CreateTypeSchemaRequest(
                        queueItem.ContentTypeAlias,
                        $"Forwarded from Umbraco: {queueItem.ContentTypeAlias}",
                        jsonSchema);

                    await apiService.CreateTypeSchemaAsync(request, stoppingToken);

                    logger.LogInformation("Created type schema '{Alias}' in Compose", queueItem.ContentTypeAlias);
                }
                else
                {
                    await apiService.UpdateTypeSchemaSchemaAsync(
                        queueItem.ContentTypeAlias,
                        jsonSchema,
                        stoppingToken);

                    logger.LogInformation("Updated type schema '{Alias}' in Compose", queueItem.ContentTypeAlias);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to forward type schema '{Alias}' to Compose", queueItem.ContentTypeAlias);
            }
        }
    }
}