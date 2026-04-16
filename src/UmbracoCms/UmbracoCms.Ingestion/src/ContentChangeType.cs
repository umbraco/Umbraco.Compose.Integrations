namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Represents different changes that need to be made in Compose based on changes in the CMS
/// </summary>
public enum ContentChangeType
{
    /// <summary>
    /// Content item has been changed in a way that doesn't affect descendants
    /// </summary>
    Update = 0,

    /// <summary>
    /// Content item has been changed in a way that affects descendants - e.g. route has been updated
    /// </summary>
    UpdateWithDescendants = 1,

    /// <summary>
    /// Content item has been deleted
    /// </summary>
    Delete = 2
}
