## About

`Umbraco.Compose.Integrations.UmbracoCms.SchemaManagement` provides Umbraco CMS schema management support for Umbraco Compose.

If you want the full integration, install the `Umbraco.Compose.Integrations.UmbracoCms` package instead.

## Key Features

- Automatically creates type schemas in Umbraco Compose based on Document Types

## How to use

To use `Umbraco.Compose.Integrations.UmbracoCms` you'll need an Umbraco Compose project with an API Application created with the `typeschema:write` scope on the environment.

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
```

In your `Program.cs`, call `.AddUmbracoComposeIngestion()` on the Umbraco builder:

```csharp
builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .AddUmbracoComposeAuthentication()

    .AddUmbracoComposeSchemaManagement()

    .Build();
```
