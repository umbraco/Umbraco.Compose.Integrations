namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

public sealed class UmbracoComposeIngestionOptions
{
    /// <summary>
    /// The collection alias to ingest content into
    /// </summary>
    public required string CollectionAlias { get; set; }
}
