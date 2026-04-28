using System.Net.Http.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;
using Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Persistence;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal sealed class SchemaBackgroundService(
    Channel<SchemaQueueItem> channel,
    IHttpClientFactory httpClientFactory,
    IServiceProvider serviceProvider,
    IScopeProvider scopeProvider,
    IOptionsFactory<JsonOptions> jsonOptionsFactory,
    ILogger<SchemaBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            SchemaQueueItem queueItem = await channel.Reader.ReadAsync(stoppingToken).ConfigureAwait(false);

            await using AsyncServiceScope serviceScope = serviceProvider.CreateAsyncScope();

            ISchemaQueueRepository queueRepository = serviceScope.ServiceProvider
                .GetRequiredService<ISchemaQueueRepository>();

            try
            {
                JsonSchemaExporterService schemaExporter = serviceScope.ServiceProvider.GetRequiredService<JsonSchemaExporterService>();
                JsonOptions jsonOptions = jsonOptionsFactory.Create(nameof(SchemaBackgroundService));

                using IScope scope = scopeProvider.CreateScope();
                IReadOnlyDictionary<string, JsonSchema> schemas = schemaExporter.GenerateSchemas(queueItem.ContentTypeAlias);
                scope.Complete();

                HttpClient client = httpClientFactory.CreateClient(nameof(SchemaBackgroundService));

                HttpResponseMessage response = await client.PutAsJsonAsync(
                    "type-schemas",
                    schemas.Select(x => new TypeSchemaDto(x.Key, null, x.Value)).ToList(),
                    jsonOptions.SerializerOptions,
                    stoppingToken)
                    .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    logger.LogDebug("Upserted type schema '{Alias}'", queueItem.ContentTypeAlias);
                }
                else
                {
                    string responseContent = await response.Content.ReadAsStringAsync(stoppingToken).ConfigureAwait(false);
                    logger.LogError(
                        "Failed to create or update type schema '{Alias}'. Status Code: {StatusCode}, Response: {ResponseBody}",
                        queueItem.ContentTypeAlias,
                        response.StatusCode,
                        responseContent);
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
