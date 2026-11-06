namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Represents different changes that need to be made in Compose based on changes in the CMS
/// </summary>
public enum MediaChangeType
{
    /// <summary>
    /// Media item has been changed
    /// </summary>
    Update = 0,

    /// <summary>
    /// Media item has been deleted
    /// </summary>
    Delete = 2
}
