using System.Text.Json;

namespace Umbraco.Compose.Cli.Mcp.Tools;

internal sealed class CallToolContext
{
    public required IReadOnlyDictionary<string, JsonElement>? Arguments { get; init; }
}
