## About

`Umbraco.Compose.Integrations.UmbracoCms.Ingestion` provides Umbraco Compose Content Ingestion support for Umbraco CMS.

If you want the full integration, install the `Umbraco.Compose.Integrations.UmbracoCms` package instead.

## Key Features

- When content is published it's automatically ingested into Umbraco Compose.
- Supports culture variations

## How to use

To use this package you will need:
- An Umbraco Compose project.
- An Environment & Collection configured for your project.
- An API Application with the `{environment}:ingestion` scope configured for the environment. API Applications can be created from the [Umbraco Cloud portal](https://www.s1.umbraco.io/).

### Installation

```shell
dotnet add package Umbraco.Compose.Integrations.UmbracoCms.Ingestion
```

### Configuration

To configure the integration, add the following to your preferred configuration source (e.g. `appsettings.json` or Azure KeyVault):

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
```

In your `Program.cs`, call `.AddUmbracoComposeIngestion()` on the Umbraco builder:

```csharp
builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .AddUmbracoComposeAuthentication()

    .AddUmbracoComposeIngestion()

    .Build();
```

## Documentation

For further information about Umbraco Compose, refer to [our documentation](https://docs.umbraco.com/umbraco-compose).
