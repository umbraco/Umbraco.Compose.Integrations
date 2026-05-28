using System.CommandLine;

namespace UmbracoCompose.Cli.Commands;

internal sealed class AgentCommand : BaseCommand
{
    public AgentCommand(IConsole console, AgentMcpCommand mcpCommand) : base("agent", "Manage AI agent configuration", console)
    {
        Subcommands.Add(mcpCommand);
    }

    protected override Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        return Task.FromResult(CommandResult.Success());
    }
}
