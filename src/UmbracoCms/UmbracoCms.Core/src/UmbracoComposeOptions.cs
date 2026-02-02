using System.ComponentModel.DataAnnotations;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core;

/// <summary>
/// Options for Umbraco Compose
/// </summary>
public sealed class UmbracoComposeOptions
{
    /// <summary>
    /// The client id of the Umbraco Compose application
    /// </summary>
    [Required]
    public string ClientId { get; set; } = default!;

    /// <summary>
    /// The client secret of the Umbraco Compose application
    /// </summary>
    [Required]
    public string ClientSecret { get; set; } = default!;

    /// <summary>
    /// The alias of the Umbraco Compose projects environment
    /// </summary>
    [Required]
    public string EnvironmentAlias { get; set; } = default!;

    /// <summary>
    /// The alias of the Umbraco Compose project
    /// </summary>
    [Required]
    public string ProjectAlias { get; set; } = default!;

    /// <summary>
    /// The region of the Umbraco Compose project
    /// </summary>
    [Required]
    public string Region { get; set; } = default!;

    /// <summary>
    /// The endpoints of the Umbraco Compose project
    /// </summary>
    [Required]
    public UmbracoComposeEndpoints Endpoints { get; set; } = new();

    /// <summary>
    /// The base URL of the Umbraco Compose management API
    /// </summary>
    public Uri GetManagementBaseUrl()
    {
        return new UriBuilder(Endpoints.ManagementUrl ?? new($"https://management.{BaseDomainHostAndPort}"))
        {
            Path = "v1",
        }.Uri;
    }

    /// <summary>
    /// The base URL of the Umbraco Compose management API
    /// </summary>
    public Uri GetManagementUrl()
    {
        return new UriBuilder(GetManagementBaseUrl())
        {
            Path = $"v1/projects/{ProjectAlias}/environments/{EnvironmentAlias}/",
        }.Uri;
    }

    /// <summary>
    /// The base URL of the Umbraco Compose ingestion API
    /// </summary>
    public Uri GetIngestionUrl()
    {
        return new UriBuilder(Endpoints.IngestionUrl ?? new($"https://ingest.{Region}.{BaseDomainHostAndPort}"))
        {
            Path = $"v1/{ProjectAlias}/{EnvironmentAlias}/",
        }.Uri;
    }

    /// <summary>
    /// The base URL of the Umbraco Compose GraphQL API
    /// </summary>
    public Uri GetGraphQLUrl()
    {
        return new UriBuilder(Endpoints.GraphQLUrl ?? new($"https://graphql.{Region}.{BaseDomainHostAndPort}"))
        {
            Path = $"{ProjectAlias}/{EnvironmentAlias}/",
        }.Uri;
    }

    /// <summary>
    /// Whether the options are valid
    /// </summary>
    public bool IsValid =>
        ClientId is not null &&
            ClientSecret is not null &&
            EnvironmentAlias is not null &&
            ProjectAlias is not null &&
            Region is not null;

    private string BaseDomainHostAndPort =>
        Endpoints.BaseDomain.GetComponents(UriComponents.HostAndPort, UriFormat.Unescaped);
}
