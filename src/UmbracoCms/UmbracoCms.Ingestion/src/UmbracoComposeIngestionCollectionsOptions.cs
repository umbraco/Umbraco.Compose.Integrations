using System.ComponentModel.DataAnnotations;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Options for Umbraco Compose ingestion collections.
/// </summary>
public sealed class UmbracoComposeIngestionCollectionsOptions
{
    /// <summary>
    /// The collection alias to ingest content into,
    /// </summary>
    [Required]
    public string Content { get; set; } = default!;

    /// <summary>
    /// The collection alias to ingest media into,
    /// </summary>
    [Required]
    public string Media { get; set; } = default!;

    ///<summary>
    /// Whether the options are valid.
    /// </summary>
    public bool IsValid =>
        Content is not null && Media is not null;
}
