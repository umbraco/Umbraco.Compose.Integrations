# Filtering and Sorting

Every Relay connection accepts `where` (filtering) and `orderBy` (sorting) arguments.

## Filtering with `where`

The `where` argument wraps type-specific filters under the **type name** (lowercased). Root-level filters (`id`, `id_any`, `variant`, `variant_any`) go directly under `where` without a type wrapper.

- **Root-level filters** (direct under `where`): `id`, `id_any`, `variant`, `variant_any`
- **Type-specific filters** (under type name): Each field of the type becomes filterable
- **Logical composition**: `AND` (all must match), `OR` (any must match), `NOT` (none must match)
- **Nested object filters**: Filter on nested properties

### Filter Patterns by Field Type

| Field Type | Filter Suffixes | Example (within type) |
|------------|----------------|----------------------|
| **String** | (none), `_any`, `_contains`, `_starts_with`, `_ends_with` | `title: "Widget"`, `title_contains: "widget"`, `tags_all: ["sale", "featured"]` |
| **Integer** | (none), `_any`, `_gt`, `_gte`, `_lt`, `_lte` | `price_gt: 10`, `price_lte: 100`, `categories_any: ["electronics", "home"]` |
| **Number** | (none), `_any`, `_gt`, `_gte`, `_lt`, `_lte` | `rating_gte: 4.5` |
| **Boolean** | (none) | `active: true` |
| **DateTime/Date/Time** | (none), `_any`, `_gt`, `_gte`, `_lt`, `_lte` | `publishedAt_gte: "2024-01-01"` |
| **Array (strings)** | `_all` (matches all), `_some` (matches any) | `tags_all: ["sale", "featured"]` |
| **Array (integers/numbers)** | `_all` (matches all), `_some` (matches any) | `categories_some: ["electronics"]` |
| **Reference (single)** | Full root filter of referenced type | `brand: { name: "Acme" }` |
| **Reference (array)** | `_all` (array subset), `_some` (array overlap) | `categories_all: [{ name: "Electronics" }]` |

### Example — Filtering with Type-Specific Patterns

```graphql
{
  products(first: 20, where: {
    # Root-level filters (direct under where)
    id: "entry-uuid-here"
    # Type-specific filters (under type name)
    product: {
      title: "Widget"
      title_contains: "widget"
      title_starts_with: "W"
      price_gt: 10
      price_lte: 100
      tags_all: ["sale", "featured"]
      publishedAt_gte: "2024-01-01"
      publishedAt_lt: "2024-12-31"
    }
  }) {
    edges {
      node {
        id
        ...on Product { title price publishedAt }
      }
    }
  }
}
```

### Example — Logical Composition with AND/OR/NOT

```graphql
{
  products(first: 20, where: {
    product: {
      AND: [
        { price_gte: 5 }
        { price_lte: 50 }
      ]
      OR: [
        { tags_all: ["sale"] }
        { tags_all: ["featured"] }
      ]
    }
  }) {
    edges {
      node {
        id
        ...on Product { title price }
      }
    }
  }
}
```

### Example — Filtering by ID

```graphql
{
  products(first: 10, where: {
    # Root-level filters go directly under where
    id: "entry-uuid-here"
    # Or match multiple IDs
    id_any: ["id-1", "id-2", "id-3"]
  }) {
    edges {
      node {
        id
        ...on Product { title }
      }
    }
  }
}
```

## Sorting with `orderBy`

The `orderBy` argument accepts a list of sort inputs, each specifying a field and direction (`ASC` or `DESC`).

- Default sort is by `id` ascending, then `variant` ascending (NULLS FIRST)
- Any field of type integer/number/string/boolean can be sorted
- Per-type sort keys are handled for polymorphic collections

### Example — Sorting

```graphql
{
  products(first: 20, orderBy: [
    { price: DESC }
    { title: ASC }
  ]) {
    edges {
      node {
        id
        ...on Product { title price }
      }
    }
  }
}
```

## Referenced Content

When a JSON Schema property has a `$ref` to another content type, it becomes:

- **Array reference** → Relay Connection with `first` and `skip` arguments (default 10)
- **Single reference** → Node field with optional `variant` argument (uses DataLoader batching)

### Example — Referenced Content

```graphql
{
  products(first: 10) {
    edges {
      node {
        id
        ...on Product {
          title
          # Array reference becomes a Relay Connection
          categories(first: 5, skip: 0) {
            edges {
              node {
                id
                ...on Category { name }
              }
            }
          }
          # Single reference with variant support
          brand(variant: "default") {
            id
            ...on Brand { name }
          }
        }
      }
    }
  }
}
```
