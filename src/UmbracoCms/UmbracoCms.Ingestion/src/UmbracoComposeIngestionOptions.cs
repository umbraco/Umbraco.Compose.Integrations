using System.ComponentModel.DataAnnotations;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Options for Umbraco Compose ingestion.
/// </summary>
public sealed class UmbracoComposeIngestionOptions
{
    /// <summary>
    /// The collection alias to ingest content into,
    /// </summary>
    [Required]
    public string CollectionAlias { get; set; } = default!;

    ///<summary>
    /// Whether the options are valid.
    /// </summary>
    public bool IsValid =>
        CollectionAlias is not null;
}
