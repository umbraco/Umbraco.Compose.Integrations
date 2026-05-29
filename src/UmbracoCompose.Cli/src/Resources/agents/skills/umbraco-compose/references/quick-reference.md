# Quick Reference

> **Agent rule:** Always include `--format Json` in CLI commands for parseable, machine-readable output.

| User says... | What it means | CLI action |
|-------------|---------------|------------|
| "ingest into the `products` collection" | Send data to the `products` collection | `umbraco-compose ingest products <data> --format Json` |
| "query the `products` collection" | Query the `products` Relay connection | `umbraco-compose graphql query '{ products(first: 20) { edges { node { id variant ...on Product { ... } } } } }' --format Json` |
| "get all products" | Paginate through the `products` connection | Use `first`/`after` with cursors from `pageInfo` |
| "what fields does the `products` type have?" | Inspect the JSON Schema for the products type | `umbraco-compose graphql introspect --format Json` or query `__schema.types` |
| "what collections are available?" | List available Relay connection fields | `umbraco-compose graphql introspect --format Json` |
| "get a single entry by ID" | Query the Node interface | `umbraco-compose graphql query '{ node(id: "...") { ...on Product { id variant ... } } }' --format Json` |
| "create a new product" | Upsert a content entry | `umbraco-compose ingest products '[{"id":"new-id","type":"product","data":{"name":"Widget"},"action":"upsert"}]' --format Json` |
| "update a product's name" | Merge-patch a content entry | `umbraco-compose ingest products '[{"id":"existing-id","action":"merge-patch","data":{"name":"New Name"}}]' --format Json` |
| "delete a product" | Delete by ID | `umbraco-compose ingest products '[{"id":"product-id","action":"delete"}]' --format Json` |
| "update just the price field" | JSON Patch (RFC 6902) | `umbraco-compose ingest products '[{"id":"product-id","action":"patch","operations":[{"op":"replace","path":"/price","value":19.99}]}]' --format Json` |
| "delete products with tag 'old'" | Delete with where condition | `umbraco-compose ingest products '[{"action":"delete","where":{"tags_some":["old"]}}]' --format Json` |
| "run a custom ingestion function" | Call function endpoint | `umbraco-compose ingest products @data.json --function-alias my-function --format Json` |
