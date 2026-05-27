## About

`Umbraco.Compose.Integrations.UmbracoCms` provides Umbraco Compose integration for Umbraco CMS. This is a convenience package that installs and registers all other Umbraco Compose CMS integrations:
- `Umbraco.Compose.Integrations.UmbracoCms.Core`
- `Umbraco.Compose.Integrations.UmbracoCms.DataSource`
- `Umbraco.Compose.Integrations.UmbracoCms.Ingestion`
- `Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement`

## How to use

To use this package you will need:
- An Umbraco Compose project.
- An Environment & Collection configured for your project.
- An API Application with the following scopes configured for the environment:
    - `{environment}:ingestion`
    - `{environment}:graphql`
    - `{environment}:typeschema:write`

API Applications can be created from the [Umbraco Cloud portal](https://www.s1.umbraco.io/).

### Installation

```shell
dotnet add package Umbraco.Compose.Integrations.UmbracoCms
```

### Configuration

To configure the integration, add the following to you preferred configuration source (e.g. `appsettings.json` or Azure KeyVault):

```json
"Umbraco:Compose": {
  "ClientId": "YOUR_API_APPLICATION_CLIENT_ID",
  "ClientSecret": "YOUR_API_APPLICATION_CLIENT_SECRET",
  "ProjectAlias": "YOUR_PROJECT_ALIAS",
  "EnvironmentAlias": "YOUR_ENVIRONMENT_ALIAS",
  "Region": "YOUR_PROJECT_REGION",
  "Ingestion": {
    "CollectionAlias": "COLLECTION_TO_INGEST_CONTENT_INTO"
  }
}

## Documentation

For further information about Umbraco Compose, refer to [our documentation](https://docs.umbraco.com/umbraco-compose).
