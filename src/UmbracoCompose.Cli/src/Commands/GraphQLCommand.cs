using System.CommandLine;

namespace UmbracoCompose.Cli.Commands;

internal sealed class GraphQLCommand(IConsole console) : BaseCommand("graphql", "Query using GraphQL", console)
{
    protected override Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
