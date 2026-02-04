using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.DataSource;

internal static class ContentGraphQLQueryProvider
{
    internal static string SearchContentQuery(
        UmbracoComposeContentPickerDataSourceConfiguration configuration)
    {
        return $$"""
          query Query($searchTerm: String, $variant: String, $after: String, $first: Int) {
            {{configuration.Collection}} (
              where: {
                AND: [
                  { {{configuration.TypeSchema}}: { {{configuration.SearchField}}_contains: $searchTerm} }
                  { variant: $variant }
                ]
              }
              after: $after,
              first: $first
            ) {
              items {
                id
                variant
                ... on {{configuration.TypeSchema.ToFirstUpper()}} {
                  {{string.Join(" ", configuration.IncludeFields)}}
                }
              }
              pageInfo{ endCursor hasNextPage }
            }
          }
        """;
    }

    internal static string ContentItemsQuery(UmbracoComposeContentPickerDataSourceConfiguration configuration)
    {
        return $$"""
          query Query($ids: [ID], $variant: String) {
            {{configuration.Collection}} (
              where: {
                AND: [
                  { {{configuration.TypeSchema}}: { id_any: $ids } }
                  { variant: $variant }
                ]
              }
            ) {
              items {
                id
                variant
                ... on {{configuration.TypeSchema.ToFirstUpper()}} {
                  {{string.Join(" ", configuration.IncludeFields)}}
                }
              }
              pageInfo{ endCursor hasNextPage }
          }
        }
        """;
    }
}
