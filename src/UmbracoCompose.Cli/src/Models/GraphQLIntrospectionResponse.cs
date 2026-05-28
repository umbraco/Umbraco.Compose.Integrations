using System.Text.Json.Serialization;

namespace UmbracoCompose.Cli.Models;

internal sealed record GraphQLData(GraphQLSchema? __schema);

internal sealed record GraphQLIntrospectionResponse(
    GraphQLData? data,
    [property: JsonPropertyName("errors")] IReadOnlyList<GraphQLResponseError>? __errors);

internal sealed record GraphQLResponseError(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("locations")] IReadOnlyList<GraphQLResponseErrorLocation>? locations);

internal sealed record GraphQLResponseErrorLocation(
    [property: JsonPropertyName("line")] int? Line,
    [property: JsonPropertyName("column")] int? Column);

internal sealed record GraphQLSchema(
    GraphQLSchemaType? queryType,
    GraphQLSchemaType? mutationType,
    IReadOnlyList<GraphQLSchemaType> types,
    IReadOnlyList<GraphQLSchemaDirective> directives,
    string? description);

internal sealed record GraphQLSchemaType(
    string? name,
    string? kind,
    string? description,
    IReadOnlyList<GraphQLSchemaField>? fields,
    IReadOnlyList<GraphQLSchemaInputValue>? inputFields,
    IReadOnlyList<GraphQLSchemaType>? interfaces,
    IReadOnlyList<GraphQLSchemaEnumValue>? enumValues,
    GraphQLSchemaTypeRef? ofType,
    [property: JsonPropertyName("specifiedByURL")] string? specifiedByURL);

internal sealed record GraphQLSchemaTypeRef(
    string? kind,
    GraphQLSchemaTypeRef? ofType);

internal sealed record GraphQLSchemaField(
    string? name,
    string? description,
    IReadOnlyList<GraphQLSchemaInputValue>? args,
    GraphQLSchemaTypeRef type,
    string? defaultValue,
    bool isDeprecated,
    string? deprecationReason);

internal sealed record GraphQLSchemaInputValue(
    string? name,
    string? description,
    GraphQLSchemaTypeRef type,
    string? defaultValue);

internal sealed record GraphQLSchemaDirective(
    string? name,
    string? description,
    IReadOnlyList<string>? locations,
    IReadOnlyList<GraphQLSchemaInputValue>? args);

internal sealed record GraphQLSchemaEnumValue(
    string? name,
    string? description,
    bool isDeprecated,
    string? deprecationReason);
