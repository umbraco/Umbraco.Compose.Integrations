using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Umbraco.Compose.Cli.Models;
using Umbraco.Compose.Cli.Services;
using Umbraco.Compose.Cli.Utilities;

namespace Umbraco.Compose.Cli.Commands;

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

    private static readonly Option<IList<string>> s_typeOption = new("--type", "-t")
    {
        Description = "Filter by type name (can be specified multiple times; shows all types when omitted)",
        Arity = ArgumentArity.OneOrMore,
        AllowMultipleArgumentsPerToken = false,
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
        Options.Add(s_typeOption);
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
                queryType {
                  name
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
                }
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
        IList<string>? typeFilter = parseResult.GetValue(s_typeOption);

        if (format == OutputFormat.Json)
        {
            GraphQLIntrospectionResponse? introspectionResponse = JsonSerializer.Deserialize(
                responseBody,
                AppJsonContext.Default.GraphQLIntrospectionResponse);

            if (introspectionResponse?.data?.__schema is null)
            {
                _logger.LogError("GraphQL introspection returned no schema. Response: {ResponseBody}", responseBody);
                return CommandResult.Failure(ExitCodes.RuntimeError, "GraphQL introspection returned no schema.");
            }

            GraphQLSchema schema = introspectionResponse.data.__schema;

            if (typeFilter is not null && typeFilter.Count > 0)
            {
                // Build filtered schema
                var filterSet = new HashSet<string>(typeFilter, StringComparer.OrdinalIgnoreCase);

                // Filter types
                var filteredTypes = schema.types
                    .Where(t => filterSet.Contains(t.name ?? ""))
                    .ToList();

                // Filter query fields
                GraphQLSchemaType? filteredQueryType = null;
                if (schema.queryType is not null)
                {
                    var filteredQueryFields = schema.queryType.fields?
                        .Where(f => IsReturnTypeMatching(f.type, filterSet))
                        .ToList();
                    if (filteredQueryFields is not null && filteredQueryFields.Count > 0)
                    {
                        filteredQueryType = new GraphQLSchemaType(
                            schema.queryType.name,
                            schema.queryType.kind,
                            schema.queryType.description,
                            filteredQueryFields,
                            schema.queryType.inputFields,
                            schema.queryType.interfaces,
                            schema.queryType.enumValues,
                            schema.queryType.ofType,
                            schema.queryType.specifiedByURL);
                    }
                }

                // Filter mutation fields
                GraphQLSchemaType? filteredMutationType = null;
                if (schema.mutationType is not null)
                {
                    var filteredMutationFields = schema.mutationType.fields?
                        .Where(f => IsReturnTypeMatching(f.type, filterSet))
                        .ToList();
                    if (filteredMutationFields is not null && filteredMutationFields.Count > 0)
                    {
                        filteredMutationType = new GraphQLSchemaType(
                            schema.mutationType.name,
                            schema.mutationType.kind,
                            schema.mutationType.description,
                            filteredMutationFields,
                            schema.mutationType.inputFields,
                            schema.mutationType.interfaces,
                            schema.mutationType.enumValues,
                            schema.mutationType.ofType,
                            schema.mutationType.specifiedByURL);
                    }
                }

                GraphQLSchema filteredSchema = new (
                    filteredQueryType,
                    filteredMutationType,
                    filteredTypes,
                    schema.directives,
                    schema.description);

                GraphQLIntrospectionResponse filteredResponse = new (
                    new GraphQLData(filteredSchema),
                    introspectionResponse.__errors);

                string json = JsonSerializer.Serialize(filteredResponse, JsonOutputHelper.Compact.GetTypeInfo(typeof(GraphQLIntrospectionResponse)));
                Console.DisplayRawText(json);
            }
            else
            {
                Console.DisplayRawText(responseBody);
            }

            return CommandResult.Success();
        }

        // Table format
        GraphQLIntrospectionResponse? tableResponse = JsonSerializer.Deserialize(
            responseBody,
            AppJsonContext.Default.GraphQLIntrospectionResponse);

        if (tableResponse?.data?.__schema is null)
        {
            _logger.LogError("GraphQL introspection returned no schema. Response: {ResponseBody}", responseBody);
            return CommandResult.Failure(ExitCodes.RuntimeError, "GraphQL introspection returned no schema.");
        }

        // Check for GraphQL errors in the response
        if (tableResponse.__errors is not null && tableResponse.__errors.Count > 0)
        {
            string errorMessages = string.Join("; ", tableResponse.__errors.Select(e => e.Message));
            _logger.LogError("GraphQL errors: {Errors}", errorMessages);
            return CommandResult.Failure(ExitCodes.RuntimeError, $"GraphQL errors: {errorMessages}");
        }

        GraphQLSchema tableSchema = tableResponse.data.__schema;
        DisplaySchemaSummary(tableSchema, typeFilter);

        return CommandResult.Success();
    }

    private void DisplaySchemaSummary(GraphQLSchema schema, IList<string>? typeFilter)
    {
        if (typeFilter is not null && typeFilter.Count > 0)
        {
            // Filtered mode: show detailed per-type tables
            DisplayFilteredSchema(schema, typeFilter);
        }
        else
        {
            // Full mode: show compact summary
            DisplayFullSchema(schema);
        }
    }

    private void DisplayFullSchema(GraphQLSchema schema)
    {
        var queryFields = schema.queryType?.fields ?? new List<GraphQLSchemaField>();

        // Query Fields
        if (queryFields.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold]Query Fields[/]");
            DisplayQueryFieldsTable(queryFields.OrderBy(f => f.name ?? "").ToList());
        }

        // Types Overview
        if (schema.types.Count > 0)
        {
            AnsiConsole.MarkupLine("");
            AnsiConsole.MarkupLine("[bold]Types[/]");

            IReadOnlyList<GraphQLSchemaType> typesToShow = schema.types.OrderBy(t => t.name ?? "").ToList();

            if (typesToShow.Count > 0)
            {
                DisplayTypesTable(typesToShow);
            }
        }
    }

    private void DisplayFilteredSchema(GraphQLSchema schema, IList<string> typeFilter)
    {
        var filterSet = new HashSet<string>(typeFilter, StringComparer.OrdinalIgnoreCase);
        var requestedTypes = schema.types
            .Where(t => filterSet.Contains(t.name ?? ""))
            .OrderBy(t => t.name ?? "")
            .ToList();

        if (requestedTypes.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No matching types found.[/]");
            return;
        }

        foreach (var type in requestedTypes)
        {
            if (type.fields is not null && type.fields.Count > 0)
            {
                AnsiConsole.MarkupLine($"[bold]{type.name.EscapeMarkup()}[/]");
                DisplayFieldsTable(type.name!, type.fields);
            }
            else if (type.enumValues is not null && type.enumValues.Count > 0)
            {
                AnsiConsole.MarkupLine($"[bold]{type.name.EscapeMarkup()}[/]");
                DisplayEnumValuesTable(type.name!, type.enumValues);
            }
        }
    }

    private void DisplayQueryFieldsTable(IReadOnlyList<GraphQLSchemaField> fields)
    {
        Table table = new();
        table.AddColumn("[bold]Name[/]");
        table.AddColumn("[bold]Type[/]");
        table.Border(TableBorder.Rounded);

        foreach (GraphQLSchemaField field in fields)
        {
            string name = field.name ?? "?";
            Text fieldName;
            if (field.isDeprecated)
            {
                fieldName = new Text(name, new Style(decoration: Decoration.Dim));
            }
            else
            {
                fieldName = new Text(name);
            }

            string typeStr = GetTypeString(field.type);
            table.AddRow(fieldName, new Text(typeStr));
        }

        Console.DisplayRenderable(table);
    }

    private void DisplayTypesTable(IReadOnlyList<GraphQLSchemaType> types)
    {
        Table table = new();
        table.AddColumn("[bold]Name[/]");
        table.AddColumn("[bold]Kind[/]");
        table.AddColumn("[bold]Fields[/]");
        table.Border(TableBorder.Rounded);

        foreach (GraphQLSchemaType type in types)
        {
            string fieldCount = type.fields is not null ? type.fields.Count.ToString() : "0";
            table.AddRow(new Text(type.name ?? "?"), new Text(type.kind ?? "?"), new Text(fieldCount));
        }

        Console.DisplayRenderable(table);
    }

    private void DisplayFieldsTable(string _typeName, IReadOnlyList<GraphQLSchemaField> fields)
    {
        Table table = new();
        table.AddColumn("[bold]Name[/]");
        table.AddColumn("[bold]Type[/]");
        table.AddColumn("[bold]Args[/]");
        table.AddColumn("[bold]Description[/]");
        table.Border(TableBorder.Rounded);

        foreach (GraphQLSchemaField field in fields)
        {
            string name = field.name ?? "?";
            Text fieldName;
            if (field.isDeprecated)
            {
                fieldName = new Text(name, new Style(decoration: Decoration.Dim));
            }
            else
            {
                fieldName = new Text(name);
            }

            string typeStr = GetTypeString(field.type);
            string argsStr = field.args is not null && field.args.Count > 0
                ? string.Join(", ", field.args.Select(a => a.name ?? "?"))
                : "-";
            string description = string.IsNullOrWhiteSpace(field.description) ? "-" : field.description!;

            table.AddRow(fieldName, new Text(typeStr), new Text(argsStr), new Text(description));
        }

        Console.DisplayRenderable(table);
    }

    private void DisplayEnumValuesTable(string _typeName, IReadOnlyList<GraphQLSchemaEnumValue> enumValues)
    {
        Table table = new();
        table.AddColumn("[bold]Name[/]");
        table.AddColumn("[bold]Description[/]");
        table.Border(TableBorder.Rounded);

        foreach (GraphQLSchemaEnumValue enumValue in enumValues)
        {
            string name = enumValue.name ?? "?";
            Text nameText;
            if (enumValue.isDeprecated)
            {
                nameText = new Text(name, new Style(decoration: Decoration.Dim));
            }
            else
            {
                nameText = new Text(name);
            }

            string description = string.IsNullOrWhiteSpace(enumValue.description) ? "-" : enumValue.description!;
            table.AddRow(nameText, new Text(description));
        }

        Console.DisplayRenderable(table);
    }

    private string GetTypeString(GraphQLSchemaTypeRef typeRef)
    {
        if (typeRef is null)
            return "?";

        string kind = typeRef.kind ?? "?";
        string suffix = "";

        switch (kind)
        {
            case "LIST":
                if (typeRef.ofType is not null)
                {
                    suffix = GetTypeString(typeRef.ofType);
                    return $"[{suffix}]";
                }
                return "[]";

            case "NON_NULL":
                if (typeRef.ofType is not null)
                {
                    string inner = GetTypeString(typeRef.ofType);
                    return inner.EndsWith("!") ? inner : $"{inner}!";
                }
                return "!";

            default:
                return typeRef.name ?? kind;
        }
    }

    private static bool IsReturnTypeMatching(GraphQLSchemaTypeRef typeRef, ISet<string> filterSet)
    {
        if (typeRef is null)
            return false;

        switch (typeRef.kind)
        {
            case "LIST":
                return typeRef.ofType is not null && IsReturnTypeMatching(typeRef.ofType, filterSet);
            case "NON_NULL":
                return typeRef.ofType is not null && IsReturnTypeMatching(typeRef.ofType, filterSet);
            default:
                return filterSet.Contains(typeRef.name ?? "");
        }
    }
}
