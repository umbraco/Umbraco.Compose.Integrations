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
    /// Media item has been changed in a way that affects descendants - e.g. moved in the tree
    /// </summary>
    UpdateWithDescendants = 1,

    /// <summary>
    /// Media item has been deleted
    /// </summary>
    Delete = 2
}
