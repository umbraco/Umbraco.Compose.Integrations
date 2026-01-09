using System.ComponentModel.DataAnnotations;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core;

public sealed class UmbracoComposeEndpoints
{
    [Required]
    public Uri BaseDomain { get; set; } = new("https://umbracocompose.com");

    public Uri? ManagementUrl { get; set; }
    public Uri? IngestionUrl { get; set; }
    public Uri? GraphQLUrl { get; set; }
}
