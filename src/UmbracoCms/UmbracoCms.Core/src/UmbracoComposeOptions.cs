using System.ComponentModel.DataAnnotations;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core;

public sealed class UmbracoComposeOptions
{
    [Required]
    public string ClientId { get; set; } = default!;

    [Required]
    public string ClientSecret { get; set; } = default!;

    [Required]
    public string EnvironmentAlias { get; set; } = default!;

    [Required]
    public string ProjectAlias { get; set; } = default!;

    public string Region { get; set; } = "germanywestcentral";

    [Required]
    public UmbracoComposeEndpoints Endpoints { get; set; } = new();

    public Uri GetManagementBaseUrl()
    {
        return new UriBuilder(Endpoints.ManagementUrl ?? new($"https://management.{BaseDomainHostAndPort}"))
        {
            Path = "v1",
        }.Uri;
    }

    public Uri GetManagementUrl()
    {
        return new UriBuilder(GetManagementBaseUrl())
        {
            Path = $"v1/projects/{ProjectAlias}/environments/{EnvironmentAlias}/",
        }.Uri;
    }

    public Uri GetIngestionUrl()
    {
        return new UriBuilder(Endpoints.IngestionUrl ?? new($"https://ingest.{Region}.{BaseDomainHostAndPort}"))
        {
            Path = $"v1/{ProjectAlias}/{EnvironmentAlias}/",
        }.Uri;
    }

    public Uri GetGraphQLUrl()
    {
        return new UriBuilder(Endpoints.GraphQLUrl ?? new($"https://graphql.{Region}.{BaseDomainHostAndPort}"))
        {
            Path = $"{ProjectAlias}/{EnvironmentAlias}/",
        }.Uri;
    }

    private string BaseDomainHostAndPort =>
        Endpoints.BaseDomain.GetComponents(UriComponents.HostAndPort, UriFormat.Unescaped);
}
