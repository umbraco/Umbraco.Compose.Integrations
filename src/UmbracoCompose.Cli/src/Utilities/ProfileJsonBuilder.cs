using System.Text.Json;
using System.Text.Json.Nodes;

namespace UmbracoCompose.Cli.Utilities;

internal static class ProfileJsonBuilder
{
    public static JsonObject ToJsonObject(string name, UmbracoCompose.Cli.Models.Profile profile, bool includeSecrets)
    {
        var obj = new JsonObject
        {
            ["name"] = name,
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

    public static JsonArray ToJsonArray(IDictionary<string, UmbracoCompose.Cli.Models.Profile> profiles, bool includeSecrets)
    {
        var arr = new JsonArray();
        foreach (var pair in profiles)
        {
            arr.Add((JsonNode)ToJsonObject(pair.Key, pair.Value, includeSecrets));
        }
        return arr;
    }

    public static string ToJsonString(JsonNode node)
    {
        return node.ToJsonString(JsonOutputHelper.Indented);
    }
}
