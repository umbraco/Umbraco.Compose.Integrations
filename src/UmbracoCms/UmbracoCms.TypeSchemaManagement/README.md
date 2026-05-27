## About

`Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement` provides Umbraco CMS schema management support for Umbraco Compose.

If you want the full integration, install the `Umbraco.Compose.Integrations.UmbracoCms` package instead.

## Key Features

- Automatically creates type schemas in Umbraco Compose based on Document Types

## How to use


To use this package you will need:
- An Umbraco Compose project.
- An Environment & Collection configured for your project.
- An API Application with the `{environment}:typeschema:write` scope configured for the environment.

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
  "Region": "YOUR_PROJECT_REGION"
}
```

In your `Program.cs`, call `.AddUmbracoComposeTypeSchemaManagement()` on the Umbraco builder:

```csharp
builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .AddUmbracoComposeAuthentication()

    .AddUmbracoComposeTypeSchemaManagement()

    .Build();
```

## Documentation

For further information about Umbraco Compose, refer to [our documentation](https://docs.umbraco.com/umbraco-compose).
