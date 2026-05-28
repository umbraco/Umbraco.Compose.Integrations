using System.CommandLine;

namespace UmbracoCompose.Cli.Commands;

internal sealed class GraphQLCommand : BaseCommand
{
    public GraphQLCommand(
        GraphQlIntrospectCommand introspectCommand,
        IConsole console) : base("graphql", "Query using GraphQL", console)
    {
        Subcommands.Add(introspectCommand);
    }

    protected override Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        return Task.FromResult(CommandResult.DisplayHelp());
    }
}
