namespace Umbraco.Compose.Integrations.UmbracoCms.DataSource;

/// <summary>
/// Defines methods for querying content items using GraphQL-based criteria, supporting both optional search terms and direct key-based
/// retrieval for specific keyed content items
/// </summary>
public interface IGraphQlContentQueryService
{
    /// <summary>
    /// Gets content items matching the specified criteria
    /// </summary>
    /// <param name="composeQueryArguments">The Compose Picker Data Source Configuration</param>
    /// <param name="paging">The paging information</param>
    /// <param name="searchTerm">An optional search term</param>
    /// <returns>The matching content items</returns>
    Task<ContentQueryResult> GetContentAsync(
        UmbracoComposeContentPickerDataSourceConfiguration composeQueryArguments,
        UmbracoComposeContentPickerDataSourcePaging paging,
        string? searchTerm);

    /// <summary>
    /// Gets content items matching the specified criteria
    /// </summary>
    /// <param name="composeQueryArguments">The Compose Picker Data Source Configuration</param>
    /// <param name="keys">The keys of the content items to retrieve</param>
    /// <returns>The matching content items</returns>
    Task<ContentQueryResult> GetContentItemsAsync(
        UmbracoComposeContentPickerDataSourceConfiguration composeQueryArguments,
        string[] keys
        );
}
