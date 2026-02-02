using Umbraco.Cms.Core.Models.DeliveryApi;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Represents a content item in the Umbraco Content Delivery API.
/// </summary>
/// <param name="content">The content item.</param>
/// <param name="culture">The culture of the content item.</param>
/// <param name="parentId">The parent ID of the content item.</param>
/// <param name="ancestors">The ancestors of the content item.</param>
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
    /// <summary>
    /// The culture of the hcontent item.
    /// </summary>
    public string Culture { get; } = culture;

    /// <summary>
    /// The ancestors of the content item.
    /// </summary>
    public Guid[] Ancestors { get; } = ancestors;

    /// <summary>
    /// The parent ID of the content item.
    /// </summary>
    public Guid? ParentId { get; } = parentId;
}
