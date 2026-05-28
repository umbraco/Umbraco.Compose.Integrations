using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace UmbracoCompose.Cli.Mcp.Tools;

internal sealed class EchoTool() : McpTool("echo")
{
    public override string? Description => "Echoes the message back to the client";

    public override ValueTask<CallToolResult> CallToolAsync(CallToolContext context, CancellationToken cancellationToken = default)
    {
        if (context.Arguments is null || !context.Arguments.TryGetValue("message", out var messageElement))
        {
            return ValueTask.FromResult(new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'message' parameter is required." }]
            });
        }

        string? message = messageElement.GetString();
        if (string.IsNullOrWhiteSpace(message))
        {
            return ValueTask.FromResult(new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'message' parameter cannot be empty." }]
            });
        }

        return ValueTask.FromResult(new CallToolResult { Content = [new TextContentBlock { Text = message }] });
    }

    public override JsonElement GetInputSchema() =>
        JsonElement.Parse("""
            {
                "type": "object",
                "properties": { "message": { "type": "string", "description": "The message to echo" } },
                "required": [ "message" ],
                "additionalProperties": false,
                "description":"Echoes the message back to the client"
            }
            """);
}
