# Schema Mappings

## Response Headers

The GraphQL endpoint returns one header to consumers:

| Header | Description |
|--------|-------------|
| `Umb-GraphQL-Query-Cost` | Query complexity score (used for rate limiting) |

Use `--debug` to see request/response headers in the logs.

## Type Schemas ↔ GraphQL Types

In Umbraco Compose, each collection has a **type schema** defined as a JSON Schema. This schema determines the structure and fields of the entries in that collection. On the GraphQL endpoint, these JSON Schemas are represented as **GraphQL types**.

| Umbraco Compose Concept | GraphQL Representation |
|------------------------|----------------------|
| JSON Schema for a collection | GraphQL object type (e.g., `Product`, `BlogPost`) |
| JSON Schema properties | GraphQL fields on the type |
| JSON Schema `required` fields | Non-nullable GraphQL fields |
| JSON Schema `type` (string, number, boolean, object, array) | GraphQL scalar or input types |

### Example — JSON Schema to GraphQL Type

A JSON Schema like:

```json
{
  "type": "object",
  "properties": {
    "title": { "type": "string" },
    "price": { "type": "number" },
    "tags": { "type": "array", "items": { "type": "string" } }
  },
  "required": ["title"]
}
```

Maps to a GraphQL type with `title` as a non-nullable string field, `price` as a number field, and `tags` as an array of strings.
