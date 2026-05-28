namespace UmbracoCompose.Cli.Models;

internal sealed class ProfileConfig
{
    public string? Default { get; set; }
    public Dictionary<string, Profile> Profiles { get; set; } = new();
}
