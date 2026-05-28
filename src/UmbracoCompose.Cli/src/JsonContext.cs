using System.Text.Json;
using System.Text.Json.Serialization;
using UmbracoCompose.Cli.Models;

namespace UmbracoCompose.Cli;

[JsonSerializable(typeof(ProfileConfig))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(GraphQLIntrospectionResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
internal sealed partial class AppJsonContext : JsonSerializerContext
{
}
