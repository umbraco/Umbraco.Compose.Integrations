# Relay Connections

All collections in Umbraco Compose are exposed as **Relay Connections** with cursor-based pagination.

## Key Rules

- Every collection field returns a **connection object**, not a list
- The connection object contains `edges` (each with a `node` and optional `cursor`) and `pageInfo`
- All nodes implement the Relay **Node interface** with fields: `id` and `variant`
- **Only `id` and `variant` can be queried directly on `node`** — all type-specific fields require inline fragments (`...on TypeName { ... }`)
- Types are **shared across collections** — a `Product` type can be used by multiple collections
- A collection can contain entries of **multiple types** — always use fragments to query type-specific fields
- Pagination uses `first` and `after` arguments on the collection field (**only forward pagination is supported**)

## Relay Connection Structure

```graphql
{
  <collectionAlias>(first: <limit>, after: "<cursor>") {
    edges {
      cursor
      node {
        id
        variant
        ...on <TypeName> {
          # type-specific fields
        }
      }
    }
    pageInfo {
      hasNextPage
      hasPreviousPage
      startCursor
      endCursor
    }
  }
}
```

## Node Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | `ID!` | The unique identifier for this content entry |
| `variant` | `String` | The variant identifier (for multi-variant content, may be null) |

## Examples

### Fetching with inline fragment

```graphql
{
  products(first: 20) {
    edges {
      cursor
      node {
        id
        variant
        ...on Product {
          title
          price
        }
      }
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

### Paginating with a cursor

```graphql
{
  products(first: 20, after: "eyJpZCI6IjEyMyJ9") {
    edges {
      cursor
      node {
        id
        variant
        ...on Product {
          title
        }
      }
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

### Fetching a single entry by ID

```graphql
{
  node(id: "<entry-id>") {
    ...on Product {
      id
      variant
      title
      price
    }
  }
}
```

### Multiple types in one collection

If a collection can contain entries of different types, use fragments for each type:

```graphql
{
  articles(first: 20) {
    edges {
      node {
        id
        variant
        ...on BlogPost {
          title
          body
          publishedAt
        }
        ...on PressRelease {
          title
          summary
          distributionList
        }
      }
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```
