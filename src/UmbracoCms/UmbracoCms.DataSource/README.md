## About

`Umbraco.Compose.Integrations.UmbracoCms.DataSource` provides Umbraco Compose Data Source support for the Umbraco CMS Entity Data Picker.

If you want the full Umbraco Compose integration, install the `Umbraco.Compose.Integrations.UmbracoCms` package instead.

## Key Features

- Create an Entity Data Picker to pick content from Umbraco Compose.
- Combines with `Umbraco.Compose.Integrations.UmbracoCms.Ingestion` to send selected Entity Data Picker entries to Compose as content references.

## How to use

To use this package you will need:
- An Umbraco Compose project.
- An Environment & Collection configured for your project.
- An API Application with `{environment}:graphql` scope configured for the environment. API Applications can be created from the [Umbraco Cloud portal](https://www.s1.umbraco.io/).

### Installation


```shell
dotnet add package Umbraco.Compose.Integrations.UmbracoCms.DataSource
```

### Configuration

To configure the integration, add the following to your preferred configuration source (e.g. `appsettings.json` or Azure KeyVault):

```json
"Umbraco:Compose": {
  "ClientId": "YOUR_API_APPLICATION_CLIENT_ID",
  "ClientSecret": "YOUR_API_APPLICATION_CLIENT_SECRET",
  "ProjectAlias": "YOUR_PROJECT_ALIAS",
  "EnvironmentAlias": "YOUR_ENVIRONMENT_ALIAS",
  "Region": "YOUR_PROJECT_REGION"
}
```


In your `Program.cs`, call `.AddUmbracoComposeDataSource()` on the Umbraco builder:

```csharp
builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .AddUmbracoComposeAuthentication()

    .AddUmbracoComposeDataSource()

    .Build();
```

## Documentation

For further information about Umbraco Compose, refer to [our documentation](https://docs.umbraco.com/umbraco-compose).
