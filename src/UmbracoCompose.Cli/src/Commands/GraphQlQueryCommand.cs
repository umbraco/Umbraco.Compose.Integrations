using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using UmbracoCompose.Cli.Services;
using UmbracoCompose.Cli.Utilities;

namespace UmbracoCompose.Cli.Commands;

internal sealed class GraphQlQueryCommand : BaseCommand
{
    private static readonly Argument<string> s_queryArgument = new("query")
    {
        Description = "GraphQL query string or file path (prefix with @ to read from file)",
    };

    private static readonly Option<string> s_profileOption = new("--profile", "-p")
    {
        Description = "The profile to use (uses default if not specified)",
    };

    private static readonly Option<OutputFormat> s_formatOption = new("--format")
    {
        Description = "Output format (default: Table). Options: Table, Json",
    };

    private static readonly Option<string> s_operationOption = new("--operation", "-o")
    {
        Description = "Operation name to execute (required for multi-operation documents)",
    };

    private static readonly Option<IList<string>> s_variableOption = new("--variable", "-V")
    {
        Description = "Variable in format name=value or name:type=value (type: string, int, float, bool, json). Can be specified multiple times or as multiple tokens per flag.",
        AllowMultipleArgumentsPerToken = true,
    };

    private static readonly Option<IList<string>> s_variablesOption = new("--variables")
    {
        Description = "Bulk variables as JSON object string or file path (prefix with @ to read from file). Overrides individual --variable flags for matching keys. Can be specified multiple times or as multiple tokens per flag.",
        AllowMultipleArgumentsPerToken = true,
    };

    private readonly ProfileResolver _profileResolver;
    private readonly GraphQLRequestExecutor _graphQLRequestExecutor;
    private readonly VariableParser _variableParser;
    private readonly ResponseFormatter _responseFormatter;
    private readonly ILogger<GraphQlQueryCommand> _logger;

    public GraphQlQueryCommand(
        IConsole console,
        ProfileResolver profileResolver,
        GraphQLRequestExecutor graphQLRequestExecutor,
        VariableParser variableParser,
        ResponseFormatter responseFormatter,
        ILogger<GraphQlQueryCommand> logger)
        : base("query", "Execute a GraphQL query against the Compose GraphQL endpoint", console)
    {
        _profileResolver = profileResolver;
        _graphQLRequestExecutor = graphQLRequestExecutor;
        _variableParser = variableParser;
        _responseFormatter = responseFormatter;
        _logger = logger;

        Arguments.Add(s_queryArgument);
        Options.Add(s_profileOption);
        Options.Add(s_formatOption);
        Options.Add(s_operationOption);
        Options.Add(s_variableOption);
        Options.Add(s_variablesOption);
    }

    protected override async Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        string query = parseResult.GetValue(s_queryArgument) ?? string.Empty;
        string? profileName = parseResult.GetValue(s_profileOption);
        string? operationName = parseResult.GetValue(s_operationOption);
        IList<string>? variableStrings = parseResult.GetValue(s_variableOption);
        IList<string>? variablesStrings = parseResult.GetValue(s_variablesOption);

        // Resolve profile
        var (resolvedName, profile, profileResult) = await _profileResolver.ResolveAsync(profileName, cancellationToken).ConfigureAwait(false);
        if (profileResult != null)
            return profileResult;

        // Read query from file or use as string
        string queryText;
        var (queryContent, queryError) = await FileReadHelper.ReadFromFileOrReturnAsync(query, cancellationToken);
        if (queryError != null)
            return queryError;
        queryText = queryContent!;

        // Validate query is not empty
        if (string.IsNullOrWhiteSpace(queryText))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, "Query cannot be empty.");
        }

        // Parse variables
        var variables = new Dictionary<string, object?>();
        CommandResult? variableError = await _variableParser.ParseAsync(variableStrings, variablesStrings, cancellationToken, variables);
        if (variableError is not null)
            return variableError;

        // Build request payload
        var payload = new Dictionary<string, object?>
        {
            ["query"] = queryText,
        };

        if (variables.Count > 0)
        {
            payload["variables"] = variables;
        }

        if (!string.IsNullOrWhiteSpace(operationName))
        {
            payload["operationName"] = operationName;
        }

        // Serialize payload to JSON
        string payloadJson = JsonSerializer.Serialize(payload, AppJsonContext.Default.DictionaryStringObject);

        // Execute GraphQL request via shared executor
        var execResult = await _graphQLRequestExecutor.ExecuteAsync(profile!, payloadJson, "application/json", cancellationToken);
        if (execResult.Failure != null)
            return execResult.Failure;

        string responseBody = execResult.Body!;

        // Output based on format
        OutputFormat format = parseResult.GetValue(s_formatOption);

        if (format == OutputFormat.Json)
        {
            // Return raw response from server, exactly as-is
            Console.DisplayRawText(responseBody);
        }
        else
        {
            // Parse response to check for errors and display table summary
            using var jsonDocument = JsonDocument.Parse(responseBody);
            JsonElement root = jsonDocument.RootElement;

            // Check for GraphQL errors
            if (root.TryGetProperty("errors", out JsonElement errorsElement) && errorsElement.ValueKind == JsonValueKind.Array)
            {
                List<string> errorMessages = new List<string>();
                foreach (JsonElement error in errorsElement.EnumerateArray())
                {
                    if (error.TryGetProperty("message", out JsonElement messageElement))
                    {
                        errorMessages.Add(messageElement.GetString() ?? "Unknown error");
                    }
                }

                if (errorMessages.Count > 0)
                {
                    string errorText = string.Join("; ", errorMessages);
                    _logger.LogError("GraphQL errors: {Errors}", errorText);
                    return CommandResult.Failure(ExitCodes.RuntimeError, $"GraphQL errors: {errorText}");
                }
            }

            // Get data element
            if (!root.TryGetProperty("data", out JsonElement dataElement) || dataElement.ValueKind == JsonValueKind.Null)
            {
                _logger.LogError("GraphQL request returned no data. Response: {ResponseBody}", responseBody);
                return CommandResult.Failure(ExitCodes.RuntimeError, "GraphQL request returned no data.");
            }

            // Display data as table
            _responseFormatter.DisplayDataTable(dataElement);
        }

        return CommandResult.Success();
    }
}
