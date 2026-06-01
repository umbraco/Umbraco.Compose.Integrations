using System.CommandLine;

namespace Umbraco.Compose.Cli.Commands;

internal sealed class GraphQLCommand : BaseCommand
{
    public GraphQLCommand(
        GraphQlIntrospectCommand introspectCommand,
        GraphQlQueryCommand queryCommand,
        IConsole console) : base("graphql", "Query using GraphQL", console)
    {
        Subcommands.Add(introspectCommand);
        Subcommands.Add(queryCommand);
    }

    protected override Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        return Task.FromResult(CommandResult.DisplayHelp());
    }
}
