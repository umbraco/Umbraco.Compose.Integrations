---
name: umbraco-compose
description: >
  Interact with the Umbraco Compose SaaS platform. Use when the user wants to ingest
  content into a collection, run GraphQL queries against Compose, or debug API issues.
  Covers the `ingest`, `graphql query`, and `graphql introspect` commands. Also use
  when the user talks about collections, content entries, or querying Compose data.
  DO NOT USE FOR: managing profiles (user-managed only), running tests, or MCP server.
license: MIT
---

# Umbraco Compose CLI

Interact with the Umbraco Compose platform using the `umbraco-compose` CLI tool.

## When to Use

- User wants to ingest content into a collection
- User wants to run a GraphQL query against the Compose GraphQL endpoint
- User wants to inspect the GraphQL schema via introspection
- User needs to debug API calls or see raw request/response bodies
- User needs to pass complex data via files or typed variables

## When Not to Use

- User wants to manage authentication profiles — profiles are user-managed. If a profile is needed but not configured, ask the user to configure one first.
- User wants to run or execute tests (use test-related skills)
- User wants to start the MCP server for AI agents (use `agent mcp`)

## API Scopes

Different operations require different API key scopes. If authentication fails, verify the API key has the required scope:

| Operation | Required Scope |
|-----------|---------------|
| GraphQL queries | `graphql` |
| GraphQL introspection | `graphql:introspection` |
| Content ingestion | `ingestion` |

The user manages their API keys and scopes. If a scope is missing, inform the user they need to generate a new API key with the required scope.

## Agent Usage

When using this skill as an AI agent, **always include `--format Json`** on every CLI command. This ensures:

- Machine-readable, parseable output
- No terminal formatting or table decorations to strip
- Errors are returned as structured JSON objects
- Reliable programmatic consumption of results

> **Rule:** Every `umbraco-compose` command you execute must include `--format Json` unless the command does not support it. The following commands support `--format Json`: `ingest`, `graphql query`, `graphql introspect`, `profiles list`, `profiles show`, and `profiles validate`.

### Why `--format Json` is required for agents

| Without `--format Json` | With `--format Json` |
|------------------------|---------------------|
| Table-formatted text output with borders and alignment | Clean, parseable JSON |
| Terminal escape codes may corrupt parsing | Pure JSON, no decorations |
| Errors go to stderr as plain text | Errors returned as JSON on stderr |
| Hard to extract values programmatically | Easy to parse with standard JSON tools |

### Example: Always use `--format Json`

```bash
# ✅ Correct — agent should always use --format Json
umbraco-compose ingest products @data.json --format Json
umbraco-compose graphql query @queries/getProducts.graphql --format Json
umbraco-compose graphql introspect --format Json

# ❌ Incorrect — table output is not suitable for agents
umbraco-compose ingest products @data.json
umbraco-compose graphql query @queries/getProducts.graphql
```

## Critical Rules

These are the most common agent mistakes. Internalize before proceeding:

| Rule | Why |
|------|-----|
| **Use `@filepath` prefix to read data from files** | Without `@`, the CLI treats the value as a literal JSON string. `@data.json` reads the file contents. |
| **Always use `--format Json` for agent operations** | Ensures parseable output. Errors always go to stderr as JSON objects. The CLI switches to stderr output when `--format Json` is active so stdout remains clean for JSON data. |
| **`--variables` overrides `--variable` for matching keys** | Bulk variables silently take precedence — no warning. |
| **Only forward pagination (`first`/`after`) is supported** | `last` and `before` are not supported. |
| **Profile management is strictly user-responsible** | Never attempt to configure or fix profiles. Tell the user to run `umbraco-compose profiles add` and `umbraco-compose profiles set-default`. |
| **Ingestion payloads are arrays of ContentEntry objects** | The batch ingestion API expects `[{ id, type, data, action }, ...]` — each entry has its own action (upsert, delete, patch, merge-patch). See `references/ingestion.md` for full details. |

## Workflow

### Step 1: Global Options

These options apply to all commands:

| Option | Description |
|--------|-------------|
| `--debug` | Enable debug logging (includes request/response bodies) |
| `--log-level <LEVEL>` / `-l <LEVEL>` | Set minimum log level: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical` |

### Step 2: Ingest Content

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

#### Batch Ingestion

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

For full details on actions, required fields, and examples, see `references/ingestion.md`.

#### Function Endpoint

Call a custom ingestion function defined on the collection:

```bash
umbraco-compose ingest products @data/function.json --function-alias my-function --format Json
```

The function endpoint accepts raw JSON (not wrapped in ContentEntry). See `references/ingestion.md` for details.

#### Action Selection Guide

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

### Step 3: GraphQL Queries

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

For Relay Connection patterns, inline fragments, filtering, sorting, and referenced content, read `references/relay-connections.md` and `references/filtering-and-sorting.md`.

### Step 4: GraphQL Introspection

```bash
umbraco-compose graphql introspect [options]
```

| Option | Description |
|--------|-------------|
| `--profile <name>` / `-p <name>` | Profile to use (uses default if not specified) |
| `--format <format>` | Output format: `Table` (default) or `Json` |
| `--type <name>` / `-t <name>` | Filter output to only show the specified type(s). Can be specified multiple times. |

> **Note:** Introspection requires a separate API key scope (`graphql-introspection`). The CLI uses a dedicated `/{project}/{environment}/__schema` endpoint. If introspection fails with an authentication error, the user needs an API key with the `graphql:introspection` scope.

#### Filtering by Type

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

## References

- For ingestion actions, ContentEntry model, and payload examples, see [references/ingestion.md](references/ingestion.md).
- For Relay Connection patterns, inline fragments, filtering, sorting, and referenced content, see [references/relay-connections.md] and [references/filtering-and-sorting.md].
- For quick user-intent-to-CLI mappings, see [references/quick-reference.md](references/quick-reference.md).
- For exit codes, see [references/exit-codes.md](references/exit-codes.md).
