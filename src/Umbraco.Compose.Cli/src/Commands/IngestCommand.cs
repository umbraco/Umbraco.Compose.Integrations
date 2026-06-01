using System.CommandLine;
using Microsoft.Extensions.Logging;
using Umbraco.Compose.Cli.Services;
using Umbraco.Compose.Cli.Utilities;

namespace Umbraco.Compose.Cli.Commands;

internal sealed class IngestCommand : BaseCommand
{
    private static readonly Argument<string> s_collectionAliasArgument = new("collection-alias")
    {
        Description = "Collection alias"
    };

    private static readonly Argument<string> s_dataArgument = new("data")
    {
        Description = "The data to ingest (JSON string or @filepath)",
    };

    private static readonly Option<string?> s_functionAliasOption = new("--function-alias")
    {
        Description = "Function alias (when present, calls the function endpoint instead of the collection endpoint)",
    };

    private static readonly Option<string> s_profileOption = new("--profile", "-p")
    {
        Description = "The profile to use (uses default if not specified)",
    };

    private static readonly Option<OutputFormat> s_formatOption = new("--format")
    {
        Description = "Output format (default: Table). Options: Table, Json",
    };

    private readonly ProfileResolver _profileResolver;
    private readonly IngestionService _ingestionService;
    private readonly ILogger<IngestCommand> _logger;

    public IngestCommand(
        IConsole console,
        ProfileResolver profileResolver,
        IngestionService ingestionService,
        ILogger<IngestCommand> logger)
        : base("ingest", "Ingest content into Umbraco Compose", console)
    {
        _profileResolver = profileResolver;
        _ingestionService = ingestionService;
        _logger = logger;

        Arguments.Add(s_collectionAliasArgument);
        Arguments.Add(s_dataArgument);

        Options.Add(s_functionAliasOption);
        Options.Add(s_formatOption);
        Options.Add(s_profileOption);
    }

    protected override async Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        string collectionAlias = parseResult.GetValue(s_collectionAliasArgument)!;
        string data = parseResult.GetValue(s_dataArgument)!;
        string? functionAlias = parseResult.GetValue(s_functionAliasOption);
        string? profileName = parseResult.GetValue(s_profileOption);

        // Resolve profile
        var (resolvedName, profile, profileResult) = await _profileResolver.ResolveAsync(profileName, cancellationToken).ConfigureAwait(false);
        if (profileResult != null)
            return profileResult;

        // Read data from file or return as-is
        var (dataContent, dataError) = await FileReadHelper.ReadFromFileOrReturnAsync(data, cancellationToken);
        if (dataError != null)
            return dataError;

        if (string.IsNullOrWhiteSpace(dataContent))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, "Data cannot be empty.");
        }

        // Call ingestion service
        var result = await _ingestionService.IngestAsync(profile!, collectionAlias, functionAlias, dataContent!, cancellationToken).ConfigureAwait(false);
        return result;
    }
}
