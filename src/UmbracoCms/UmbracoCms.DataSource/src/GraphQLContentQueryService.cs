using System.Net.Http.Json;
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
        string queryText = ContentGraphQLQueryProvider.ContentItemsQuery(composeQueryArguments);
        return ExecuteQueryAsync(
            composeQueryArguments.Collection,
            queryText,
            new() {
                { "ids", keys },
                { "variant", string.IsNullOrEmpty(composeQueryArguments.Variant) ? null :  composeQueryArguments.Variant},
            });
    }

    public Task<ContentQueryResult> GetContentAsync(
        UmbracoComposeContentPickerDataSourceConfiguration composeQueryArguments,
        UmbracoComposeContentPickerDataSourcePaging paging,
        string? searchTerm)
    {
        string queryText = ContentGraphQLQueryProvider.SearchContentQuery(composeQueryArguments);
        return ExecuteQueryAsync(
            composeQueryArguments.Collection,
            queryText,
            new() {
                { "searchTerm", searchTerm ?? string.Empty },
                { "variant", string.IsNullOrEmpty(composeQueryArguments.Variant) ? null :  composeQueryArguments.Variant},
                { "after", paging.After },
                { "first", paging.Take },
            });
    }

    private async Task<ContentQueryResult> ExecuteQueryAsync(string collectionName, string queryText, Dictionary<string, object?> variables)
    {
        try
        {
            HttpClient httpClient = _httpClientFactory.CreateClient(nameof(GraphQLContentQueryService));
            HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                string.Empty,
                new
                {
                    query = queryText,
                    variables,
                })
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return ContentQueryResult.Error($"An error occurred while searching for content: {content}");
            }

            JsonElement responseContent = await response.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
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
