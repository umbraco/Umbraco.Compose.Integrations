using System.Text.Json;
using System.Text.Json.Serialization;
using UmbracoCompose.Cli.Models;

namespace UmbracoCompose.Cli;

[JsonSerializable(typeof(ProfileConfig))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(GraphQLIntrospectionResponse))]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(IList<string>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
internal sealed partial class AppJsonContext : JsonSerializerContext
{
}
