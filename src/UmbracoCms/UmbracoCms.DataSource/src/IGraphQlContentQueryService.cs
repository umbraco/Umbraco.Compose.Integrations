namespace Umbraco.Compose.Integrations.UmbracoCms.DataSource;

/// <summary>
/// Defines methods for querying content items using GraphQL-based criteria, supporting both optional search terms and direct key-based
/// retrieval for specific keyed content items
/// </summary>
public interface IGraphQlContentQueryService
{
    Task<ContentQueryResult> GetContentAsync(
        UmbracoComposeContentPickerDataSourceConfiguration composeQueryArguments,
        UmbracoComposeContentPickerDataSourcePaging paging,
        string? searchTerm);

    Task<ContentQueryResult> GetContentItemsAsync(
        UmbracoComposeContentPickerDataSourceConfiguration composeQueryArguments,
        string[] keys
        );
}
