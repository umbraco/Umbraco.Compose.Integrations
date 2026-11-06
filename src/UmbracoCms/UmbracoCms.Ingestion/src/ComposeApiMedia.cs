using Umbraco.Cms.Core.Models.DeliveryApi;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Represents a content item in the Umbraco Media Delivery API.
/// </summary>
/// <param name="media">The media item.</param>
/// <param name="parentId">The parent ID of the media item.</param>
/// <param name="ancestors">The ancestors of the media item.</param>
public sealed class ComposeApiMedia(
    IApiMedia media,
    Guid? parentId,
    Guid[] ancestors
) : IApiMedia
{
    /// <summary>
    /// The ancestors of the content item.
    /// </summary>
    public Guid[] Ancestors { get; } = ancestors;

    /// <summary>
    /// The parent ID of the content item.
    /// </summary>
    public Guid? ParentId { get; } = parentId;

    /// <inheritdoc />
    public Guid Id =>
        media.Id;

    /// <inheritdoc />
    public string Name =>
        media.Name;

    /// <inheritdoc />
    public string MediaType =>
        media.MediaType;

    /// <inheritdoc />
    public string Url =>
        media.Url;

    /// <inheritdoc />
    public string? Extension =>
        media.Extension;

    /// <inheritdoc />
    public int? Width =>
        media.Width;

    /// <inheritdoc />
    public int? Height =>
        media.Height;

    /// <inheritdoc />
    public int? Bytes =>
        media.Bytes;

    /// <inheritdoc />
    public IDictionary<string, object?> Properties =>
        media.Properties;
}
