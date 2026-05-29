using System.CommandLine;

namespace UmbracoCompose.Cli.Commands;

internal sealed class AgentCommand : BaseCommand
{
    public AgentCommand(IConsole console, AgentMcpCommand mcpCommand, AgentInitCommand initCommand) : base("agent", "Manage AI agent configuration", console)
    {
        Subcommands.Add(mcpCommand);
        Subcommands.Add(initCommand);
    }

    protected override Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        return Task.FromResult(CommandResult.Success());
    }
}
