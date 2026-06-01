# CLI Reference

> **Agent rule:** Always include `--format Json` in CLI commands for parseable, machine-readable output.

## Global Options

These options apply to all commands:

| Option | Description |
|--------|-------------|
| `--debug` | Enable debug logging (includes request/response bodies) |
| `--log-level <LEVEL>` / `-l <LEVEL>` | Set minimum log level: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical` |

## Ingest Content

```bash
umbraco-compose ingest <collection-alias> <data> [options]
```

| Argument | Description |
|----------|-------------|
| `<collection-alias>` | The collection alias to ingest into |
| `<data>` | JSON data to ingest. Use `@filepath` to read from a file |

| Option | Description |
|--------|-------------|
| `--function-alias <alias>` | Call the function endpoint instead of batch |
| `--profile <name>` / `-p <name>` | Profile to use (uses default if not specified) |
| `--format <format>` | Output format: `Table` (default) or `Json` |

### Batch Ingestion

The batch endpoint accepts a JSON array of `ContentEntry` objects. Each entry specifies an action:

```bash
umbraco-compose ingest products @data/batch.json --format Json
```

Example `batch.json`:
```json
[
  { "id": "abc-123", "type": "product", "data": { "name": "Widget", "price": 9.99 }, "action": "upsert" },
  { "id": "def-456", "action": "delete" }
]
```

### Function Endpoint

Call a custom ingestion function defined on the collection:

```bash
umbraco-compose ingest products @data/function.json --function-alias my-function --format Json
```

### Action Selection Guide

| User intent | Action |
|-------------|--------|
| Create or update a full entry | `upsert` |
| Remove a single entry by ID | `delete` (with `id`) |
| Remove entries matching criteria | `delete` (with `where`) |
| Update specific fields (full replacement) | `patch` (JSON Patch) |
| Update specific fields (partial update) | `merge-patch` |
| Custom processing logic | Function endpoint with `--function-alias` |

Examples:

```bash
umbraco-compose ingest products @data/batch.json --format Json
umbraco-compose ingest products @data/function.json --function-alias my-function --format Json
umbraco-compose --debug ingest products @data.json --profile staging --format Json
```

## GraphQL Queries

```bash
umbraco-compose graphql query <query> [options]
```

| Argument | Description |
|----------|-------------|
| `<query>` | GraphQL query string. Use `@filepath` to read from a file |

| Option | Description |
|--------|-------------|
| `--profile <name>` / `-p <name>` | Profile to use (uses default if not specified) |
| `--format <format>` | Output format: `Table` (default) or `Json` |
| `--operation <name>` / `-o <name>` | Operation name (required for multi-operation documents) |
| `--variable <name=value>` / `-V <name=value>` | Individual variable. Can be specified multiple times |
| `--variables <json>` | Bulk variables as JSON string or `@filepath`. Overrides individual variables for matching keys. |

Examples:

```bash
umbraco-compose graphql query @queries/getProducts.graphql --format Json
umbraco-compose graphql query @queries/getProduct.graphql --variable id:int=123 --format Json
umbraco-compose graphql query @queries/search.graphql --variables @variables/search.json --format Json
```

## GraphQL Introspection

```bash
umbraco-compose graphql introspect [options]
```

| Option | Description |
|--------|-------------|
| `--profile <name>` / `-p <name>` | Profile to use (uses default if not specified) |
| `--format <format>` | Output format: `Table` (default) or `Json` |
| `--type <name>` / `-t <name>` | Filter output to only show the specified type(s). Can be specified multiple times. |

> **Note:** Introspection requires a separate API key scope (`graphql-introspection`). If introspection fails with an authentication error, the user needs an API key with the `graphql:introspection` scope.

### Filtering by Type

Use `--type` (or `-t`) to narrow introspection output to specific types. This is useful when you only need to inspect a handful of types instead of the entire schema.

- Specify multiple types by repeating the flag: `--type Product --type BlogPost`
- Query fields are filtered to only those whose return type matches a requested type
- Types table shows only the requested types with their full field details
- Works with both `--format Table` and `--format Json`
- With `--format Json`, returns only the filtered schema (compact, non-indented)

```bash
# Filter to a single type
umbraco-compose graphql introspect --type Product --format Json

# Filter to multiple types
umbraco-compose graphql introspect --type Product --type BlogPost --format Json
umbraco-compose graphql introspect -t Product -t BlogPost --format Json
```

## Profile Resolution

1. If `--profile <name>` is specified, use that profile
2. If no profile specified and a default is configured, use the default
3. If no default is configured, inform the user:

> "The Umbraco Compose CLI requires a configured profile to authenticate. Please configure one by running:
>
> ```bash
> umbraco-compose profiles add <name> <region> <project-alias> <environment-alias>
> ```
>
> Then set it as default with:
>
> ```bash
> umbraco-compose profiles set-default <name>
> ```"
