using System.Text.Json;
using System.Text.Json.Serialization;
using UmbracoCompose.Cli.Clients.Ingestion.Models;
using UmbracoCompose.Cli.Models;

namespace UmbracoCompose.Cli;

[JsonSerializable(typeof(ProfileConfig))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(GraphQLIntrospectionResponse))]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(IList<string>))]
[JsonSerializable(typeof(List<ContentEntry>))]
[JsonSerializable(typeof(List<ProblemDetails_Error>))]
[JsonSerializable(typeof(JsonElement))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
internal sealed partial class AppJsonContext : JsonSerializerContext;
