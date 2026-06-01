using System.Text.Json;
using System.Text.Json.Nodes;

namespace UmbracoCompose.Cli.Utilities;

internal static class ProfileJsonBuilder
{
    public static JsonObject ToJsonObject(UmbracoCompose.Cli.Models.Profile profile, bool includeSecrets)
    {
        var obj = new JsonObject
        {
            ["region"] = profile.Region,
            ["projectAlias"] = profile.ProjectAlias,
            ["environmentAlias"] = profile.EnvironmentAlias,
        };
        if (includeSecrets)
        {
            obj["clientId"] = profile.ClientId;
            obj["clientSecret"] = profile.ClientSecret;
        }
        return obj;
    }

    public static JsonObject ToJsonObject(IDictionary<string, UmbracoCompose.Cli.Models.Profile> profiles, bool includeSecrets)
    {
        var obj = new JsonObject();
        foreach (var pair in profiles)
        {
            obj[pair.Key] = ToJsonObject(pair.Value, includeSecrets);
        }
        return obj;
    }

    public static string ToJsonString(JsonNode node)
    {
        return node.ToJsonString(JsonOutputHelper.Compact);
    }
}
