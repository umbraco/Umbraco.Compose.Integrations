using Umbraco.Cms.Core.Models;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal interface IContentTypeSchemaService
{
    internal ContentTypeSchemaInfo? GetDocumentTypeByAlias(string alias);
}
