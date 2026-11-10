using Umbraco.Cms.Core;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal record SchemaQueueItem(Guid Id, StringUdi ContentTypeAlias);
