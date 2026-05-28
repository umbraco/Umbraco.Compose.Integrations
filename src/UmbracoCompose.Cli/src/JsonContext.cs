using System.Text.Json;
using System.Text.Json.Serialization;
using UmbracoCompose.Cli.Models;

namespace UmbracoCompose.Cli;

[JsonSerializable(typeof(Profile))]
[JsonSerializable(typeof(ProfileConfig))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
internal sealed partial class AppJsonContext : JsonSerializerContext
{
}
