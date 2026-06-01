---
name: umbraco-compose
description: >
  Interact with the Umbraco Compose SaaS platform. Use when the user wants to ingest
  content into a collection, run GraphQL queries against Compose, or debug API issues.
  Covers the `ingest`, `graphql query`, and `graphql introspect` commands. Also use
  when the user talks about collections, content entries, GraphQL, Relay connections,
  filtering, sorting, or querying Compose data.
license: MIT
compatibility: Requires the `umbraco-compose` CLI tool to be installed and available in PATH
metadata:
  version: "1.0"
  author: umbraco-compose-team
---

# Umbraco Compose CLI

Interact with the Umbraco Compose platform using the `umbraco-compose` CLI tool.

## When to Use

- User wants to ingest content into a collection
- User wants to run a GraphQL query against the Compose GraphQL endpoint
- User wants to inspect the GraphQL schema via introspection
- User needs to debug API calls or see raw request/response bodies
- User needs to pass complex data via files or typed variables
- User talks about collections, content entries, or querying Compose data

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

> **Rule:** Every `umbraco-compose` command you execute must include `--format Json` unless the command does not support it.

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
| **Ingestion payloads are arrays of ContentEntry objects** | The batch ingestion API expects `[{ id, type, data, action }, ...]` — each entry has its own action (upsert, delete, patch, merge-patch). |

## CLI Reference

For full command syntax, flags, arguments, and examples, see [references/cli-reference.md](references/cli-reference.md).

Key commands:

- `umbraco-compose ingest <collection> <data> --format Json` — ingest content
- `umbraco-compose graphql query <query> --format Json` — run GraphQL queries
- `umbraco-compose graphql introspect --format Json` — inspect the schema

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

- For full CLI command syntax, flags, arguments, and examples, see [references/cli-reference.md](references/cli-reference.md).
- For ingestion actions, ContentEntry model, and payload examples, see [references/ingestion.md](references/ingestion.md).
- For Relay Connection patterns, inline fragments, filtering, sorting, and referenced content, see [references/relay-connections.md] and [references/filtering-and-sorting.md].
- For quick user-intent-to-CLI mappings, see [references/quick-reference.md](references/quick-reference.md).
- For exit codes, see [references/exit-codes.md](references/exit-codes.md).
