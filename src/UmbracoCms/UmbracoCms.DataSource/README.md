## About

`Umbraco.Compose.Integrations.UmbracoCms.DataSource` provides Umbraco Compose Data Source support for Umbraco CMS, which provides the `Umbraco Compose` data source to be selected as a data source for an `Entity Data Picker`

If you want the full Umbraco Compose integration, install the `Umbraco.Compose.Integrations.UmbracoCms` package instead.

## Key Features

- The ability to create an `Entity Data Picker` with the `Umbraco Compose` data source.

## How to use

To use the `Umbraco.Compose.Integrations.UmbracoCms.DataSource` you'll need an Umbraco Compose project with an API Application created with the `graphql` scope on the environment or project. The compose project will need a `TypeSchema` configured with content ingested that conforms to the schema

### Installation


```shell
dotnet add package Umbraco.Compose.Integrations.UmbracoCms.DataSource
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
