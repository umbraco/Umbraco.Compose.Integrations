using System.ComponentModel.DataAnnotations;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core;

/// <summary>
/// The endpoints of the Umbraco Compose instance.
/// </summary>
public sealed class UmbracoComposeEndpoints
{
    /// <summary>
    /// The base domain of the Umbraco Compose instance.
    /// Defaults to <c>umbracocompose.com</c>
    /// </summary>
    /// <remarks>
    /// This usually shouldn't be changed.
    /// </remarks>
    [Required]
    public Uri BaseDomain { get; set; } = new("https://umbracocompose.com");

    /// <summary>
    /// The Management URL of the Umbraco Compose instance.
    /// </summary>
    /// <remarks>
    /// This usually shouldn't be set.
    /// </remarks>
    public Uri? ManagementUrl { get; set; }

    /// <summary>
    /// The Ingestion URL of the Umbraco Compose instance.
    /// </summary>
    /// <remarks>
    /// This usually shouldn't be set.
    /// </remarks>
    public Uri? IngestionUrl { get; set; }

    /// <summary>
    /// The GraphQL URL of the Umbraco Compose instance.
    /// </summary>
    /// <remarks>
    /// This usually shouldn't be set.
    /// </remarks>
    public Uri? GraphQLUrl { get; set; }
}
