using Umbraco.Extensions;

namespace Umbraco.Compose.Integrations.UmbracoCms.DataSource;

internal static class ContentGraphQLQueryProvider
{
    internal static string SearchContentQuery(
        UmbracoComposeContentPickerDataSourceConfiguration configuration,
        UmbracoComposeContentPickerDataSourcePaging paging,
        string? searchTerm)
    {
#pragma warning disable RCS0056 // A line is too long
        return $$"""
        query Q {
            {{configuration.Collection}} (where: {{{configuration.TypeSchema}}: {{{CollectionSearchFilter(configuration, searchTerm)}}} }, {{AfterCursor(paging)}}) {
                items {
                    ... on {{configuration.TypeSchema.ToFirstUpper()}} {
                         id
                         variant
                         {{string.Join("\n", configuration.IncludeFields)}}
                    }
                }
                        
                pageInfo{ endCursor hasNextPage }
            }
        }
        """;
#pragma warning restore RCS0056 // A line is too long
    }

    internal static string ContentItemsQuery(UmbracoComposeContentPickerDataSourceConfiguration configuration, string[] keys)
    {
        return $$"""
        query Q {
            {{configuration.Collection}} (where: { {{configuration.TypeSchema}}: {{{IdLookupFilter(configuration, keys)}}} }) {
                items {
                    ... on {{configuration.TypeSchema.ToFirstUpper()}} {
                         id
                         variant
                         {{string.Join("\n", configuration.IncludeFields)}}
                    }
                }
                        
                pageInfo{ endCursor hasNextPage }
            }
        }
        """;
    }

    private static string IdLookupFilter(UmbracoComposeContentPickerDataSourceConfiguration configuration, string[] keys)
    {
        string? variantFilter = VariantFilter(configuration.Variant);
        string idsFormatted = string.Join(", ", keys.Select(id => $$""" "{{id}}" """));
        string idFilter = $"{configuration.KeyField}_any: [{idsFormatted}]";

        return $"{variantFilter}, {idFilter}";
    }

    private static string CollectionSearchFilter(
        UmbracoComposeContentPickerDataSourceConfiguration configuration,
        string? searchTerm)
    {
        string? variantFilter = VariantFilter(configuration.Variant);
        string? searchFilter = SearchFieldFilter(configuration.SearchField, searchTerm);
        string?[] allFilters = [variantFilter, searchFilter,];

        return string.Join(',', allFilters.Where(x => x is not null));
    }

    private static string? VariantFilter(string? variant)
    {
        return string.IsNullOrWhiteSpace(variant)
            ? "variant: null"
            : $$"""variant: "{{variant}}" """;
    }

    private static string? AfterCursor(UmbracoComposeContentPickerDataSourcePaging paging)
    {
        return string.IsNullOrEmpty(paging.After)
            ? $"first: {paging.Take}"
            : $$"""first: {{paging.Take}} after: "{{paging.After}}" """;
    }

    private static string? SearchFieldFilter(string? searchField, string? searchTerm)
    {
        if (string.IsNullOrEmpty(searchField) || string.IsNullOrEmpty(searchTerm))
        {
            return null;
        }

        return $$"""{{searchField}}_contains: "{{searchTerm}}" """;
    }
}
