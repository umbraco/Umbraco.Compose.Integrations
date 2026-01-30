using Umbraco.Cms.Core.Models;

namespace Umbraco.Compose.Integrations.UmbracoCms.Schema;

internal interface IContentTypeSchemaService
{
    internal ContentTypeSchemaInfo GetDocumentTypeByAlias(string alias);
}