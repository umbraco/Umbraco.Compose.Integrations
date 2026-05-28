using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace UmbracoCompose.Cli.Mcp.Tools;

internal abstract class McpTool
{
    protected McpTool(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public virtual string? Description { get; }

    public abstract ValueTask<CallToolResult> CallToolAsync(CallToolContext context, CancellationToken cancellationToken = default);

    public abstract JsonElement GetInputSchema();
}
