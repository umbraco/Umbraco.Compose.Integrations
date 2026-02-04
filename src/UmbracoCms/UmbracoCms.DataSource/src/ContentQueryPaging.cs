namespace Umbraco.Compose.Integrations.UmbracoCms.DataSource;

/// <summary>
/// Represents the paging information for a query result. The EndCursor can be provided to a subsequent query to retrieve the next page of results, if HasNextPage is true.
/// </summary>
public sealed class ContentQueryPaging
{
    /// <summary>
    /// The cursor representing the end position of the current page of results.
    /// </summary>
    public string EndCursor { get; set; } = string.Empty;

    /// <summary>
    /// The value indicating whether there are more pages of results available beyond the current page.
    /// </summary>
    public bool HasNextPage { get; set; }
}
