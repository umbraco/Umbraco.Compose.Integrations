using System.CommandLine;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using UmbracoCompose.Cli.Models;
using UmbracoCompose.Cli.Services;

namespace UmbracoCompose.Cli.Commands;

internal sealed class GraphQlIntrospectCommand : BaseCommand
{
    private static readonly Option<string> s_profileOption = new("--profile", "-p")
    {
        Description = "The profile to use (uses default if not specified)",
    };

    private static readonly Option<OutputFormat> s_formatOption = new("--format")
    {
        Description = "Output format (Table or Json)",
    };

    private readonly ProfileConfigService _profileConfigService;
    private readonly IOAuthService _oAuthService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GraphQlIntrospectCommand> _logger;

    public GraphQlIntrospectCommand(
        IConsole console,
        ProfileConfigService profileConfigService,
        IOAuthService oAuthService,
        HttpClient httpClient,
        ILogger<GraphQlIntrospectCommand> logger)
        : base("introspect", "Run GraphQL introspection against the Compose GraphQL endpoint", console)
    {
        _profileConfigService = profileConfigService;
        _oAuthService = oAuthService;
        _httpClient = httpClient;
        _logger = logger;

        Options.Add(s_profileOption);
        Options.Add(s_formatOption);
    }

    protected override async Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        string? profileName = parseResult.GetValue(s_profileOption);

        ProfileConfig? config = _profileConfigService.Load();

        if (config is null || config.Profiles.Count == 0)
        {
            return CommandResult.Failure(ExitCodes.ValidationError, "No profiles configured. Add a profile first with 'profiles add'.");
        }

        // Resolve profile: explicit name > default
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

        if (!config.Profiles.TryGetValue(resolvedName, out UmbracoCompose.Cli.Models.Profile? profile))
        {
            return CommandResult.Failure(ExitCodes.ValidationError, $"Profile '{resolvedName}' not found.");
        }

        // Validate profile fields before constructing URL
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

        // Construct GraphQL URL
        string graphQLUrl = $"https://graphql.{profile.Region}.umbracocompose.com/{profile.ProjectAlias}/{profile.EnvironmentAlias}/";

        // Standard GraphQL introspection query
        string introspectionQuery = """
            {
              __schema {
                queryType { name }
                mutationType { name }
                types {
                  kind
                  name
                  description
                  fields(includeDeprecated: true) {
                    name
                    description
                    args {
                      name
                      description
                      type { kind, name, ofType { kind, name } }
                      defaultValue
                    }
                    type { kind, name, ofType { kind, name } }
                    isDeprecated
                    deprecationReason
                  }
                  inputFields {
                    name
                    description
                    type { kind, name, ofType { kind, name } }
                    defaultValue
                  }
                  interfaces { kind, name, ofType { kind, name } }
                  enumValues(includeDeprecated: true) {
                    name
                    description
                    isDeprecated
                    deprecationReason
                  }
                }
                directives {
                  name
                  description
                  locations
                  args {
                    name
                    description
                    type { kind, name, ofType { kind, name } }
                    defaultValue
                  }
                }
              }
            }
            """;

        HttpResponseMessage? response = null;

        try
        {
            // Get bearer token
            TokenResponse token = await _oAuthService.AuthenticateAsync(profile.ClientId, profile.ClientSecret, cancellationToken);

            // POST to GraphQL endpoint with per-request auth header
            var requestContent = new StringContent(introspectionQuery, Encoding.UTF8, "application/graphql");
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
                Console.DisplayRawText(responseBody);
            }
            else
            {
                GraphQLIntrospectionResponse? introspectionResponse = JsonSerializer.Deserialize(
                    responseBody,
                    AppJsonContext.Default.GraphQLIntrospectionResponse);

                if (introspectionResponse?.data?.__schema is null)
                {
                    _logger.LogError("GraphQL introspection returned no schema. Response: {ResponseBody}", responseBody);
                    return CommandResult.Failure(ExitCodes.RuntimeError, "GraphQL introspection returned no schema.");
                }

                // Check for GraphQL errors in the response
                if (introspectionResponse.__errors is not null && introspectionResponse.__errors.Count > 0)
                {
                    var errorMessages = string.Join("; ", introspectionResponse.__errors.Select(e => e.Message));
                    _logger.LogError("GraphQL errors: {Errors}", errorMessages);
                    return CommandResult.Failure(ExitCodes.RuntimeError, $"GraphQL errors: {errorMessages}");
                }

                GraphQLSchema schema = introspectionResponse.data.__schema;
                DisplaySchemaSummary(schema);
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
                catch
                {
                    // Ignore read errors
                }
            }

            var statusCode = ex.StatusCode ?? HttpStatusCode.ServiceUnavailable;

            if (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                _logger.LogError("Authentication failed for GraphQL endpoint. Response: {ResponseBody}", responseBody);
                return CommandResult.Failure(ExitCodes.ValidationError, "Authentication failed. Check your profile credentials.");
            }

            _logger.LogError("GraphQL request failed with status {StatusCode}. Response: {ResponseBody}", statusCode, responseBody);
            return CommandResult.Failure(ExitCodes.RuntimeError, $"GraphQL request failed ({statusCode}).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to GraphQL endpoint");
            return CommandResult.Failure(ExitCodes.RuntimeError, "Failed to connect to GraphQL endpoint.");
        }
    }

    private void DisplaySchemaSummary(GraphQLSchema schema)
    {
        int deprecatedCount = 0;

        foreach (GraphQLSchemaType type in schema.types)
        {
            if (type.fields is not null)
            {
                foreach (GraphQLSchemaField field in type.fields)
                {
                    if (field.isDeprecated == true)
                    {
                        deprecatedCount++;
                    }
                }
            }

            if (type.enumValues is not null)
            {
                foreach (GraphQLSchemaEnumValue enumValue in type.enumValues)
                {
                    if (enumValue.isDeprecated == true)
                    {
                        deprecatedCount++;
                    }
                }
            }
        }

        // Build table
        Table table = new();
        table.AddColumn("Field");
        table.AddColumn("Value");
        table.Border(TableBorder.Rounded);

        table.AddRow("Query Type", schema.queryType?.name ?? "N/A");

        string mutationTypeName = schema.mutationType?.name ?? "None";
        table.AddRow("Mutation Type", mutationTypeName);

        table.AddRow("Types", schema.types.Count.ToString());

        table.AddRow("Directives", schema.directives.Count.ToString());

        table.AddRow("Deprecated", deprecatedCount.ToString());

        Console.DisplayRenderable(table);
    }
}
