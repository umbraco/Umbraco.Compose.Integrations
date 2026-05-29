using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using UmbracoCompose.Cli.Models;
using UmbracoCompose.Cli.Services;
using UmbracoCompose.Cli.Utilities;

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

    private readonly ProfileResolver _profileResolver;
    private readonly GraphQLRequestExecutor _graphQLRequestExecutor;
    private readonly ILogger<GraphQlIntrospectCommand> _logger;

    public GraphQlIntrospectCommand(
        IConsole console,
        ProfileResolver profileResolver,
        GraphQLRequestExecutor graphQLRequestExecutor,
        ILogger<GraphQlIntrospectCommand> logger)
        : base("introspect", "Run GraphQL introspection against the Compose GraphQL endpoint", console)
    {
        _profileResolver = profileResolver;
        _graphQLRequestExecutor = graphQLRequestExecutor;
        _logger = logger;

        Options.Add(s_profileOption);
        Options.Add(s_formatOption);
    }

    protected override async Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        string? profileName = parseResult.GetValue(s_profileOption);

        // Resolve profile
        var (_, profile, profileResult) = await _profileResolver.ResolveAsync(profileName, cancellationToken).ConfigureAwait(false);
        if (profileResult != null)
            return profileResult;

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

        // Execute GraphQL request via shared executor
        var result = await _graphQLRequestExecutor.ExecuteAsync(profile!, introspectionQuery, "application/graphql", cancellationToken);
        if (result.Failure != null)
            return result.Failure;

        string responseBody = result.Body!;

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
                string errorMessages = string.Join("; ", introspectionResponse.__errors.Select(e => e.Message));
                _logger.LogError("GraphQL errors: {Errors}", errorMessages);
                return CommandResult.Failure(ExitCodes.RuntimeError, $"GraphQL errors: {errorMessages}");
            }

            GraphQLSchema schema = introspectionResponse.data.__schema;
            DisplaySchemaSummary(schema);
        }

        return CommandResult.Success();
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
