using Umbraco.Cms.Core.Models.DeliveryApi;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

public sealed class ComposeApiContent(
    IApiContent content,
    string culture,
    Guid? parentId,
    Guid[] ancestors
) : ApiContent(
    content.Id,
    content.Name ?? string.Empty,
    content.ContentType,
    content.CreateDate,
    content.UpdateDate,
    content.Route,
    content.Properties)
{
    public string Culture { get; } = culture;
    public Guid[] Ancestors { get; } = ancestors;
    public Guid? ParentId { get; } = parentId;
}
