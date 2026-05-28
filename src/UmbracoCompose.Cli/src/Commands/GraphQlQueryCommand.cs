uing System.CommandLine;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using UmbracoCompose.Cli.Models;
using UmbracoCompose.Cli.Services;

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

    private readonly ProfileConfigService _profileConfigService;
    private readonly IOAuthService _oAuthService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GraphQlQueryCommand> _logger;

    public GraphQlQueryCommand(
        IConsole console,
        ProfileConfigService profileConfigService,
        IOAuthService oAuthService,
        HttpClient httpClient,
        ILogger<GraphQlQueryCommand> logger)
        : base("query", "Execute a GraphQL query against the Compose GraphQL endpoint", console)
    {
        _profileConfigService = profileConfigService;
        _oAuthService = oAuthService;
        _httpClient = httpClient;
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
        ProfileConfig? config = _profileConfigService.Load();

        if (config is null || config.Profiles.Count == 0)
        {
            return CommandResult.Failure(ExitCodes.ValidationError, "No profiles configured. Add a profile first with 'profiles add'.");
        }

        string? resolvedName = profileName;

        if (string.IsNullOrWhiteSpace(resolvedName))
        {
            if (!string.IsNullOrWhiteSpace(config.Default) && config.Profiles.ContainsKey(config.Default))
            {
                resolvedName = config.Default;
            }

            if (resolvedName is null)
            {
                return CommandResult.Failure(ExitCodes.ValidationError, "Default profile is not configured.");
            }
        }

        if (!config.Profiles.TryGetValue(resolvedName, out var profile))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, $"Profile '{resolvedName}' not found.");
        }

        if (string.IsNullOrWhiteSpace(profile.Region))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, "Profile is missing 'Region'.");
        }

        if (string.IsNullOrWhiteSpace(profile.ProjectAlias))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, "Profile is missing 'ProjectAlias'.");
        }

        if (string.IsNullOrWhiteSpace(profile.EnvironmentAlias))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, "Profile is missing 'EnvironmentAlias'.");
        }

        // Build GraphQL URL
        string graphQLUrl = $"https://graphql.{profile.Region}.umbracocompose.com/{profile.ProjectAlias}/{profile.EnvironmentAlias}/";

        // Read query from file or use as string
        string queryText;
        if (query.StartsWith("@", StringComparison.Ordinal))
        {
            string filePath = query[1..];
            try
            {
                queryText = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            }
            catch (FileNotFoundException)
            {
                return CommandResult.Failure(ExitCodes.ValidationError, $"Query file not found: {filePath}");
            }
            catch (IOException ex)
            {
                return CommandResult.Failure(ExitCodes.ValidationError, $"Failed to read query file: {ex.Message}");
            }
        }
        else
        {
            queryText = query;
        }

        // Validate query is not empty
        if (string.IsNullOrWhiteSpace(queryText))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, "Query cannot be empty.");
        }

        // Parse variables
        Dictionary<string, object?> variables = new();

        // First, parse individual --variable flags
        if (variableStrings is not null)
        {
            foreach (string variable in variableStrings)
            {
                CommandResult? result = ParseVariable(variable, variables);
                if (result is not null)
                {
                    return result;
                }
            }
        }

        // Then, parse --variables JSON (overrides individual variables for same keys)
        if (variablesStrings is not null)
        {
            foreach (string variablesEntry in variablesStrings)
            {
                string jsonContent;
                if (variablesEntry.StartsWith("@", StringComparison.Ordinal))
                {
                    string filePath = variablesEntry[1..];
                    try
                    {
                        jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
                    }
                    catch (FileNotFoundException)
                    {
                        return CommandResult.Failure(ExitCodes.ValidationError, $"Variables file not found: {filePath}");
                    }
                    catch (IOException ex)
                    {
                        return CommandResult.Failure(ExitCodes.ValidationError, $"Failed to read variables file: {ex.Message}");
                    }
                }
                else
                {
                    jsonContent = variablesEntry;
                }

                try
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonContent, AppJsonContext.Default.DictionaryStringObject);
                    if (parsed is not null)
                    {
                        foreach (var kvp in parsed)
                        {
                            variables[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    return CommandResult.Failure(ExitCodes.ValidationError, $"Failed to parse variables JSON: {ex.Message}");
                }
            }
        }

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

        HttpResponseMessage? response = null;

        try
        {
            // Get bearer token
            TokenResponse token;
            try
            {
                token = await _oAuthService.AuthenticateAsync(profile.ClientId, profile.ClientSecret, cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                _logger.LogError(ex, "Authentication failed for GraphQL endpoint");
                return CommandResult.Failure(ExitCodes.ValidationError, "Authentication failed. Check your profile credentials.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to obtain authentication token");
                return CommandResult.Failure(ExitCodes.ValidationError, "Failed to obtain authentication token. Check your network connection.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to obtain authentication token");
                return CommandResult.Failure(ExitCodes.ValidationError, "Failed to obtain authentication token.");
            }

            // Serialize payload
            string payloadJson = JsonSerializer.Serialize(payload, AppJsonContext.Default.DictionaryStringObject);

            // POST to GraphQL endpoint
            var requestContent = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, graphQLUrl)
            {
                Content = requestContent,
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/graphql-response+json"));
            request.Headers.Add("GraphQL-Require-Preflight", "true");

            response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string? errorBody = null;
                try
                {
                    errorBody = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while reading body");
                }

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    _logger.LogError("Authentication failed for GraphQL endpoint. Response: {ResponseBody}", errorBody);
                    return CommandResult.Failure(ExitCodes.ValidationError, "Authentication failed. Check your profile credentials.");
                }

                _logger.LogError("GraphQL request failed with status {StatusCode}. Response: {ResponseBody}", response.StatusCode, errorBody);
                return CommandResult.Failure(ExitCodes.RuntimeError, $"GraphQL request failed ({response.StatusCode}).");
            }

            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

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
                var root = jsonDocument.RootElement;

                // Check for GraphQL errors
                if (root.TryGetProperty("errors", out JsonElement errorsElement) && errorsElement.ValueKind == JsonValueKind.Array)
                {
                    var errorMessages = new List<string>();
                    foreach (var error in errorsElement.EnumerateArray())
                    {
                        if (error.TryGetProperty("message", out JsonElement messageElement))
                        {
                            errorMessages.Add(messageElement.GetString() ?? "Unknown error");
                        }
                    }

                    if (errorMessages.Count > 0)
                    {
                        var errorText = string.Join("; ", errorMessages);
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
                DisplayDataTable(dataElement);
            }

            return CommandResult.Success();
        }
        catch (HttpRequestException ex)
        {
            string? responseBody = null;
            if (response is not null)
            {
                try
                {
                    responseBody = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
                }
                catch (Exception readEx)
                {
                    _logger.LogDebug(readEx, "Could not read error response body");
                }
            }

            if (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                _logger.LogError("Authentication failed for GraphQL endpoint. Response: {ResponseBody}", responseBody);
                return CommandResult.Failure(ExitCodes.ValidationError, "Authentication failed. Check your profile credentials.");
            }

            _logger.LogError("GraphQL request failed with status {StatusCode}. Response: {ResponseBody}", ex.StatusCode, responseBody);
            return CommandResult.Failure(ExitCodes.RuntimeError, $"GraphQL request failed ({ex.StatusCode}).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute GraphQL query");
            return CommandResult.Failure(ExitCodes.RuntimeError, "Failed to execute GraphQL query.");
        }
    }

    private static CommandResult? ParseVariable(string variable, Dictionary<string, object?> variables)
    {
        int equalsIndex = variable.IndexOf('=');
        if (equalsIndex <= 0)
        {
            return CommandResult.Failure(ExitCodes.ValidationError, $"Invalid variable format '{variable}'. Expected name=value or name:type=value.");
        }

        string keyWithType = variable[..equalsIndex];
        string value = variable[(equalsIndex + 1)..];

        string name;
        string type;

        int colonIndex = keyWithType.IndexOf(':');
        if (colonIndex > 0)
        {
            name = keyWithType[..colonIndex];
            type = keyWithType[(colonIndex + 1)..].ToLowerInvariant();
            if (string.IsNullOrEmpty(type))
            {
                return CommandResult.Failure(ExitCodes.ValidationError, $"Invalid variable '{variable}'. Type specifier is empty. Valid types: string, int, float, bool, json.");
            }
        }
        else if (colonIndex == 0)
        {
            return CommandResult.Failure(ExitCodes.ValidationError, $"Invalid variable '{variable}'. Variable name is empty.");
        }
        else
        {
            name = keyWithType;
            type = "string";
        }

        object? typedValue;
        try
        {
            typedValue = type switch
            {
                "int" => int.Parse(value, CultureInfo.InvariantCulture),
                "float" => double.Parse(value, CultureInfo.InvariantCulture),
                "bool" => bool.Parse(value),
                "json" => JsonSerializer.Deserialize<object?>(value, AppJsonContext.Default.Object) ?? throw new JsonException("JSON value deserialized to null. Ensure the JSON is a valid object or array."),
                _ => value, // string
            };
        }
        catch (FormatException ex) when (type is "int" or "float" or "bool")
        {
            return CommandResult.Failure(ExitCodes.ValidationError, $"Invalid {type} value '{value}' for variable '{name}': {ex.Message}");
        }
        catch (JsonException ex) when (type == "json")
        {
            return CommandResult.Failure(ExitCodes.ValidationError, $"Invalid JSON value for variable '{name}': {ex.Message}");
        }

        variables[name] = typedValue;
        return null;
    }

    private void DisplayDataTable(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object)
        {
            Console.DisplayRawText(data.GetRawText());
            return;
        }

        var rootProperties = data.EnumerateObject().ToList();

        if (rootProperties.Count == 1)
        {
            // Single root field — recursively search for arrays
            var field = rootProperties[0];
            FindAndDisplayFirstArray(field.Name, field.Value);
        }
        else
        {
            // Multiple root fields — show table
            Table table = new();
            table.AddColumn("Field");
            table.AddColumn("Type");
            table.AddColumn("Preview");
            table.Border(TableBorder.Rounded);

            foreach (var field in rootProperties)
            {
                table.AddRow(
                    field.Name.EscapeMarkup(),
                    GetValueKind(field.Value),
                    GetPreview(field.Value)
                );
            }

            Console.DisplayRenderable(table);
        }
    }

    private void FindAndDisplayFirstArray(string path, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() > 0)
        {
            DisplayArrayAsTable(path, element);
            return;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                var childPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
                FindAndDisplayFirstArray(childPath, prop.Value);
                return; // Found and displayed — stop searching
            }
        }

        // No array found at any depth — show scalar
        Console.DisplayRawText($"{path}: {GetPreview(element)}");
    }

    private void DisplayArrayAsTable(string name, JsonElement array)
    {
        int count = array.GetArrayLength();

        if (count == 0)
        {
            Console.DisplayRawText($"{name}: [] (empty array)");
            return;
        }

        // Check if items are objects to build a proper table
        var firstItem = array.EnumerateArray().FirstOrDefault();

        if (firstItem.ValueKind == JsonValueKind.Object)
        {
            // Always show objects as a table
            var sampleProps = firstItem.EnumerateObject().Take(8).ToList();

            Table table = new();
            table.AddColumn("#");
            foreach (var prop in sampleProps)
            {
                table.AddColumn(prop.Name.EscapeMarkup());
            }
            table.Border(TableBorder.Rounded);

            foreach (var item in array.EnumerateArray())
            {
                var rowValues = new List<string> { "#" + (table.Rows.Count + 1) };
                foreach (var prop in sampleProps)
                {
                    if (item.TryGetProperty(prop.Name, out var v))
                    {
                        rowValues.Add(GetPreview(v));
                    }
                    else
                    {
                        rowValues.Add("?");
                    }
                }
                table.AddRow(rowValues.ToArray());
            }

            Console.DisplayRenderable(table);
        }
        else
        {
            // Simple array — show values
            Console.DisplayRawText($"{name}: [{string.Join(", ", array.EnumerateArray().Select(GetPreview))}]");
        }
    }

    private void DisplayScalar(string name, JsonElement value)
    {
        Console.DisplayRawText($"{name.EscapeMarkup()}: {GetPreview(value)}");
    }

    private static string GetPreview(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => EscapeJsonString(element.GetString()),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            JsonValueKind.Object => $"{{...{element.GetPropertyCount()} props...}}",
            JsonValueKind.Array => $"[{element.GetArrayLength()} items]",
            _ => element.GetRawText(),
        };
    }

    private static string EscapeJsonString(string? value)
    {
        if (value is null) return "null";
        var sb = new StringBuilder(value.Length + 2);
        sb.Append('"');
        foreach (var c in value)
        {
            sb.Append(c switch
            {
                '\\' => "\\\\",
                '"' => "\\\"",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ => c,
            });
        }
        sb.Append('"');
        return sb.ToString();
    }

    private static string GetValueKind(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => "String",
            JsonValueKind.Number => "Number",
            JsonValueKind.True or JsonValueKind.False => "Boolean",
            JsonValueKind.Null => "Null",
            JsonValueKind.Object => "Object",
            JsonValueKind.Array => "Array",
            _ => "Unknown",
        };
    }
}
