using System.ComponentModel.DataAnnotations;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core;

/// <summary>
/// Options for Umbraco Compose
/// </summary>
public sealed class UmbracoComposeOptions
{
    private Func<PublishedItemType, string, string> _typeSchemaAliasResolver =
        (_, contentTypeAlias) => contentTypeAlias;

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
    /// Sets a delegate that resolves a type schema alias from the content type alias.
    /// </summary>
    /// <remarks>
    /// The default implementation returns the same alias which can cause conflicts if content and media types have the same alias.
    /// </remarks>
    /// <param name="resolver">The resolver method.</param>
    /// <returns>The current instance of <see cref="UmbracoComposeOptions" />.</returns>
    public UmbracoComposeOptions SetTypeSchemaAliasResolver(Func<PublishedItemType, string, string> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        _typeSchemaAliasResolver = resolver;
        return this;
    }

    /// <summary>
    /// Gets a type schema alias for the content type alias.
    /// </summary>
    /// <param name="publishedItemType">The content type item type.</param>
    /// <param name="contentTypeAlias">The content type alias to resolve the type schema alias for.</param>
    public string GetTypeSchemaAlias(PublishedItemType publishedItemType, string contentTypeAlias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentTypeAlias);

        return _typeSchemaAliasResolver(publishedItemType, contentTypeAlias);
    }

    /// <summary>
    /// The base URL of the Umbraco Compose management API
    /// </summary>
    public Uri GetManagementBaseUrl()
    {
        return new UriBuilder(Endpoints.ManagementUrl ?? new($"https://management.{BaseDomainHostAndPort}"))
        {
            Path = "v1"
        }.Uri;
    }

    /// <summary>
    /// The base URL of the Umbraco Compose management API
    /// </summary>
    public Uri GetManagementUrl()
    {
        return new UriBuilder(GetManagementBaseUrl())
        {
            Path = $"v1/projects/{ProjectAlias}/environments/{EnvironmentAlias}/"
        }.Uri;
    }

    /// <summary>
    /// The base URL of the Umbraco Compose ingestion API
    /// </summary>
    public Uri GetIngestionUrl()
    {
        return new UriBuilder(Endpoints.IngestionUrl ?? new($"https://ingest.{Region}.{BaseDomainHostAndPort}"))
        {
            Path = $"v1/{ProjectAlias}/{EnvironmentAlias}/"
        }.Uri;
    }

    /// <summary>
    /// The base URL of the Umbraco Compose GraphQL API
    /// </summary>
    public Uri GetGraphQLUrl()
    {
        return new UriBuilder(Endpoints.GraphQLUrl ?? new($"https://graphql.{Region}.{BaseDomainHostAndPort}"))
        {
            Path = $"{ProjectAlias}/{EnvironmentAlias}/"
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
