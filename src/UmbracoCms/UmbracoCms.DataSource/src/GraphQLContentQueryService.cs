using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Umbraco.Compose.Integrations.UmbracoCms.DataSource;

internal sealed class GraphQLContentQueryService(
    IHttpClientFactory httpClientFactory,
    ILogger<GraphQLContentQueryService> logger) : IGraphQlContentQueryService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public Task<ContentQueryResult> GetContentItemsAsync(
        UmbracoComposeContentPickerDataSourceConfiguration composeQueryArguments,
        string[] keys)
    {
        string queryText = ContentGraphQLQueryProvider.ContentItemsQuery(composeQueryArguments, keys);
        return ExecuteQueryAsync(composeQueryArguments.Collection, queryText);
    }

    public Task<ContentQueryResult> GetContentAsync(
        UmbracoComposeContentPickerDataSourceConfiguration composeQueryArguments,
        UmbracoComposeContentPickerDataSourcePaging paging,
        string? searchTerm)
    {
        string queryText = ContentGraphQLQueryProvider.SearchContentQuery(composeQueryArguments, paging, searchTerm);
        return ExecuteQueryAsync(composeQueryArguments.Collection, queryText);
    }

    private async Task<ContentQueryResult> ExecuteQueryAsync(string collectionName, string queryText)
    {
        try
        {
            HttpClient httpClient = _httpClientFactory.CreateClient(nameof(GraphQLContentQueryService));
            StringContent content = new(queryText);
            content.Headers.ContentType = new("application/graphql");
            HttpResponseMessage response = await httpClient.PostAsync(string.Empty, content).ConfigureAwait(false);

            string contentString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            JsonElement responseContent = JsonSerializer.Deserialize<JsonElement>(contentString);

            if (!response.IsSuccessStatusCode)
            {
                return ContentQueryResult.Error($"An error occurred while searching for content: {contentString}");
            }

            bool hasResponseData = responseContent.TryGetProperty("data", out JsonElement responseData);
            if (!hasResponseData)
            {
                return ContentQueryResult.Error("No response content");
            }

            JsonElement collectionItems = responseData.GetProperty(collectionName);
            GraphQlItems? result = collectionItems.Deserialize<GraphQlItems>();

            return ContentQueryResult.Ok(
                result?.Items ?? [],
                new ContentQueryPaging
                {
                    EndCursor = result?.PageInfo?.EndCursor ?? string.Empty,
                    HasNextPage = result?.PageInfo?.HasNextPage ?? false,
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while querying Umbraco Compose GraphQL");
            return ContentQueryResult.Error(ex.Message);
        }
    }

    public sealed class GraphQlPageInfo
    {
        [JsonPropertyName("endCursor")]
        public string EndCursor { get; set; } = string.Empty;

        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }
    }

    public sealed class GraphQlItems
    {
        [JsonPropertyName("items")]
        public object[]? Items { get; set; }

        [JsonPropertyName("pageInfo")]
        public GraphQlPageInfo? PageInfo { get; set; }
    }
}
