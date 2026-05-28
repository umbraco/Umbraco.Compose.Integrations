using System.CommandLine;

namespace UmbracoCompose.Cli.Commands;

public class ProfileListCommand : Command
{
    public ProfileListCommand() : base("list", "List configured profiles")
    {
    }
}
