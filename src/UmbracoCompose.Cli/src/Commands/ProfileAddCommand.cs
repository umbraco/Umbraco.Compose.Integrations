using System.CommandLine;

namespace UmbracoCompose.Cli.Commands;

public class ProfileAddCommand : Command
{
    private static readonly Argument<string> s_nameArgument = new("name")
    {
        Description = "Name of the profile"
    };

    private static readonly Argument<string> s_regionArgument = new("region")
    {
        Description = "Region"
    };

    private static readonly Argument<string> s_projectAliasArgument = new("project-alias")
    {
        Description = "Project alias"
    };

    private static readonly Argument<string> s_environmentAliasArgument = new("environment-alias")
    {
        Description = "Environment alias"
    };

    public ProfileAddCommand() : base("add", "Add a new profile")
    {
        Arguments.Add(s_nameArgument);
        Arguments.Add(s_regionArgument);
        Arguments.Add(s_projectAliasArgument);
        Arguments.Add(s_environmentAliasArgument);
    }
}
