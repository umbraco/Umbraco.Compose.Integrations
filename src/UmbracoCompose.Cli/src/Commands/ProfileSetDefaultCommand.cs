using System.CommandLine;

namespace UmbracoCompose.Cli.Commands;

internal class ProfileSetDefaultCommand : BaseCommand
{
    public ProfileSetDefaultCommand(IConsole console) : base("set-default", "Set the default profile", console)
    {
    }

    protected override Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
