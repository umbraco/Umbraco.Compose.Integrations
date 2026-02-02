## About

`Umbraco.Compose.Integrations.UmbracoCms.Core` provides a set of shared functionalities for integrating Umbraco CMS with Umbraco Compose.

If you want the full integration, install the `Umbraco.Compose.Integrations.UmbracoCms` package instead.

## How to Use

To use `Umbraco.Compose.Integrations.UmbracoCms.Core` you'll need an Umbraco Compose project with an API Application created.

### Installation

```shell
dotnet add package Umbraco.Compose.Integrations.UmbracoCms.Core
```

### Configuration

To configure the integrations, add the following to you preferred configuration source (e.g. `appsettings.json` or Azure KeyVault):

```json
"Umbraco:Compose": {
  "ClientId": "YOUR_API_APPLICATION_CLIENT_ID",
  "ClientSecret": "YOUR_API_APPLICATION_CLIENT_SECRET",
  "ProjectAlias": "YOUR_PROJECT_ALIAS",
  "EnvironmentAlias": "YOUR_ENVIRONMENT_ALIAS",
  "Region": "YOUR_PROJECT_REGION"
}
```

In your `Program.cs`, call `.AddUmbracoComposeAuthentication()` on the Umbraco builder:

```csharp
builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()

    .AddUmbracoComposeAuthentication()

    .Build();
```
