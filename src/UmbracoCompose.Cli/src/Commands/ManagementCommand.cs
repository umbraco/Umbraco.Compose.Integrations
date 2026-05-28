using System.CommandLine;

namespace UmbracoCompose.Cli.Commands;

internal sealed class ManagementCommand(IConsole console) : BaseCommand("management", "Management your project", console)
{
    protected override Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
