using System.CommandLine;

namespace UmbracoCompose.Cli.Commands;

internal sealed class IngestCommand : BaseCommand
{
    private static readonly Argument<string> s_environmentAliasArgument = new("environment-alias")
    {
        Description = "Environment alias"
    };

    private static readonly Argument<string> s_collectionAliasArgument = new("collection-alias")
    {
        Description = "Collection alias"
    };

    private static readonly Argument<string?> s_functionOptionALias= new("function-alias")
    {
        Description = "Function alias",
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Argument<string?> s_dataArgument = new("data")
    {
        Description = "The data to ingest",
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Option<string> s_profileOption = new("--profile", "-p")
    {
        Description = "The profile to use",
    };

    private static readonly Option<OutputFormat> s_formatOption = new("--format")
    {
        Description = "Output format (Table or Json)",
    };

    public IngestCommand(IConsole console) : base("ingest", "Ingest content", console)
    {
        Arguments.Add(s_environmentAliasArgument);
        Arguments.Add(s_collectionAliasArgument);
        Arguments.Add(s_functionOptionALias);
        Arguments.Add(s_dataArgument);

        Options.Add(s_formatOption);
        Options.Add(s_profileOption);
    }

    protected override Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
