using Microsoft.Extensions.DependencyInjection;
using Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Migrations;
using Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence;
using Umbraco.Compose.Integrations.UmbracoCms.QueuePersistence.Persistence.Repositories;

#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace Umbraco.Cms.Core.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
///     Extension methods for the core Umbraco Compose queue persistence services.
/// </summary>
public static class UmbracoBuilderExtensions
{
    extension(IUmbracoBuilder builder)
    {
        /// <summary>
        ///     Adds the queue persistence tables and repositories
        /// </summary>
        public IUmbracoBuilder AddUmbracoComposeQueuePersistence()
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Services.AddSingleton<IContentQueueRepository, ContentQueueRepository>();
            builder.Services.AddSingleton<ISchemaQueueRepository, SchemaQueueRepository>();
            builder.AddComponent<QueuePersistenceMigrationComponent>();
            return builder;
        }
    }
}
