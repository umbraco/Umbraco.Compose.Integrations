using System.Text.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.DataSource;

internal sealed class GraphQLContentQueryService : IGraphQlContentQueryService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GraphQLContentQueryService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<ContentQueryResult> GetContentItemsAsync(
        UmbracoComposeContentPickerDataSourceConfiguration composeQueryArguments,
        string[] keys)
    {
        var queryText = ContentGraphQLQueryProvider.ContentItemsQuery(composeQueryArguments, keys);
        return ExecuteQueryAsync(composeQueryArguments.Collection, queryText);
    }

    public Task<ContentQueryResult> GetContentAsync(
        UmbracoComposeContentPickerDataSourceConfiguration composeQueryArguments,
        UmbracoComposeContentPickerDataSourcePaging paging,
        string? searchTerm)
    {
        var queryText = ContentGraphQLQueryProvider.SearchContentQuery(composeQueryArguments, paging, searchTerm);
        return ExecuteQueryAsync(composeQueryArguments.Collection, queryText);
    }

    private async Task<ContentQueryResult> ExecuteQueryAsync(string collectionName, string queryText)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient(nameof(GraphQLContentQueryService));
            var content = new StringContent(queryText);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/graphql");
            var response = await httpClient.PostAsync(string.Empty, content);

            var contentString = await response.Content.ReadAsStringAsync();
            var responseContent = JsonSerializer.Deserialize<JsonElement>(contentString);

            if (!response.IsSuccessStatusCode)
            {
                return ContentQueryResult.Error($"An error occurred while searching for content: {contentString}");
            }

            var hasResponseData = responseContent.TryGetProperty("data", out var responseData);
            if (!hasResponseData)
            {
                return ContentQueryResult.Error("No response content");
            }

            var collectionItems = responseData.GetProperty(collectionName);
            var result = JsonSerializer.Deserialize<GraphQlItems>(collectionItems);

            return ContentQueryResult.Ok(
                result?.items ?? [],
                new ContentQueryPaging
                {
                    EndCursor = result?.pageInfo.endCursor ?? string.Empty,
                    HasNextPage = result?.pageInfo.hasNextPage ?? false
                });
        }
        catch (Exception e)
        {
            return ContentQueryResult.Error(e.Message);
        }
    }

    private sealed class GraphQlPageInfo
    {
        public string endCursor { get; set; } = string.Empty;
        public bool hasNextPage { get; set; } = false;
    }

    private sealed class GraphQlItems
    {
        public object[] items { get; set; } = [];
        public GraphQlPageInfo pageInfo { get; set; } = default!;
    }
}
