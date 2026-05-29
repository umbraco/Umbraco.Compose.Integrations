# Ingestion Reference

## Overview

The Ingestion API allows you to insert, update, delete, and partially update content entries in a collection. It supports two endpoints:

1. **Batch endpoint** — for structured operations with explicit actions (upsert, delete, patch, merge-patch)
2. **Function endpoint** — for custom ingestion logic defined on the collection

## API Key Scope

Required scope: `ingestion`

## Batch Endpoint

**`PUT /v1/{projectAlias}/{environmentAlias}/{collectionAlias}`**

Accepts an array of `ContentEntry` objects. Each entry specifies an action that determines how the data is processed.

### ContentEntry Model

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | string | Always | Unique identifier for the content entry. Omit when using `where` conditions. |
| `type` | string | Upsert only | The type/schema alias for the content entry. Required for `upsert` actions. |
| `variant` | string? | Optional | Language/culture variant (e.g., `en-GB`). Cannot be used with `where` conditions. |
| `data` | object | Upsert, MergePatch | The full content data (upsert) or merge-patch data. |
| `action` | enum | Always | One of: `upsert`, `delete`, `patch`, `merge-patch`. |
| `operations` | array | Patch only | JSON Patch operations array (RFC 6902). Each operation has `op`, `path`, and `value`. |
| `where` | object | Delete only | Conditional match criteria for bulk deletion. See [Where Conditions](#where-conditions) below. |

### Actions

#### `upsert`

Inserts a new content entry or updates an existing one.

- **Required fields**: `id`, `type`, `data`, `action: "upsert"`
- **Optional fields**: `variant`
- **Forbidden fields**: `operations`, `where`

Example:
```json
{
  "id": "04c4b6a1-1639-471d-b8a6-2b394a071646",
  "type": "software",
  "data": { "name": "Umbraco Forms" },
  "action": "upsert"
}
```

With variant:
```json
{
  "id": "176fa4c5-5ae9-457c-adf8-5826824cad63",
  "type": "software",
  "data": { "name": "Umbraco Deploy" },
  "variant": "en-GB",
  "action": "upsert"
}
```

#### `delete`

Deletes a content entry. Two modes:

**By ID** (single entry):
- **Required fields**: `id`, `action: "delete"`
- **Optional fields**: `variant`
- **Forbidden fields**: `type`, `data`, `operations`, `where`

Example:
```json
{
  "id": "456",
  "action": "delete"
}
```

**Conditional** (multiple entries matching criteria):
- **Required fields**: `where`, `action: "delete"`
- **Forbidden fields**: `id`, `type`, `data`, `operations`, `variant`

Example:
```json
{
  "action": "delete",
  "where": {
    "author": { "familyName": "Herbert" },
    "tags_some": [ "this", "or-this" ]
  }
}
```

#### `patch`

Applies JSON Patch operations (RFC 6902) to an existing content entry.

- **Required fields**: `id`, `operations`, `action: "patch"`
- **Optional fields**: `variant`
- **Forbidden fields**: `type`, `data`, `where`

The `operations` field must be a JSON array of patch operations, each with `op`, `path`, and `value`:

Example:
```json
{
  "id": "my-content",
  "action": "patch",
  "operations": [
    { "op": "replace", "path": "/name", "value": "Welcome" },
    { "op": "remove", "path": "/prices[0]" }
  ]
}
```

#### `merge-patch`

Applies a JSON Merge Patch (RFC 7396) to an existing content entry. Sets or updates the specified fields.

- **Required fields**: `id`, `data`, `action: "merge-patch"`
- **Optional fields**: `variant`
- **Forbidden fields**: `type`, `operations`, `where`

Example:
```json
{
  "id": "my-content",
  "action": "merge-patch",
  "data": {
    "title": "Hello!",
    "phoneNumber": "+01-123-456-7890",
    "author": { "familyName": null },
    "tags": [ "example" ]
  }
}
```

### Where Conditions

Used only with `delete` actions for conditional bulk deletion.

- Must be a JSON object with at least one property
- Property names cannot be empty
- Arrays in conditions use operation suffixes:
  - `_some` — match if any element in the array matches (e.g., `tags_some: ["tag1", "tag2"]`)
- Arrays must contain at least one element

Example:
```json
{
  "where": {
    "author": { "familyName": "Herbert" },
    "tags_some": [ "this", "or-this" ]
  }
}
```

### Batch Payload Structure

The CLI sends data as a JSON array of `ContentEntry` objects. When using `@filepath`, the file should contain:

```json
[
  { "id": "...", "type": "...", "data": { ... }, "action": "upsert" },
  { "id": "...", "action": "delete" }
]
```

## Function Endpoint

**`PUT|POST /v1/{projectAlias}/{environmentAlias}/{collectionAlias}/{ingestionFunctionAlias}`**

Calls a custom ingestion function defined on the collection. Accepts raw JSON data (not wrapped in `ContentEntry`).

- Supports both `PUT` and `POST` (for compatibility with clients that can only send one)
- The ingestion function alias must exist for the collection
- Request body is a single JSON object

Example:
```json
{
  "id": "53853400-d0bf-4275-9734-5232504205b6",
  "contentTypeAlias": "service",
  "variant": "en-GB",
  "name": "Umbraco Support"
}
```

### CLI Usage

Use the `--function-alias` option to call the function endpoint instead of the batch endpoint:

```bash
umbraco-compose ingest <collection-alias> <data> --function-alias <alias>
```

## Error Responses

The API returns:

- **404 Not Found** — project, environment, or collection not found; or ingestion function alias doesn't exist
- **400 Bad Request** — validation errors (invalid action, missing required fields, invalid JSON schema)

Errors are returned as `ProblemDetails` objects with an `errors` array.

## Choosing the Right Action

| User intent | Recommended action |
|-------------|-------------------|
| Create or update a full entry | `upsert` |
| Remove a single entry by ID | `delete` (with `id`) |
| Remove entries matching criteria | `delete` (with `where`) |
| Update specific fields (full replacement) | `patch` (JSON Patch) |
| Update specific fields (partial update) | `merge-patch` |
| Custom processing logic | Function endpoint with `--function-alias` |
