using System.CommandLine;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Umbraco.Compose.Cli.Mcp.Tools;

namespace Umbraco.Compose.Cli.Commands;

internal sealed class AgentMcpCommand : BaseCommand
{
    private readonly ILoggerFactory _loggerFactory;

    private readonly Dictionary<string, McpTool> _tools = new();

    public AgentMcpCommand(IConsole console, ILoggerFactory loggerFactory) : base("mcp", "Start the MCP (Model Context Protocol) server", console)
    {
        _loggerFactory = loggerFactory;
    }

    protected override async Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {

        _tools["echo"] = new EchoTool();

        McpServerOptions options = new()
        {
            ServerInfo = new()
            {
                Name = "umbraco-compose-mcp-server",
                Version = "0.1", // TODO: Extract from constant or smth
            },
            Handlers = new()
            {
                CallToolHandler = CallToolHandlerAsync,
                ListToolsHandler = ListToolsHandlerAsync,
            },
        };

        StdioServerTransport transport = new(options, _loggerFactory);
        McpServer server = McpServer.Create(transport, options, _loggerFactory);

        await server.RunAsync(cancellationToken).ConfigureAwait(false);

        return CommandResult.Success();
    }

    private ValueTask<CallToolResult> CallToolHandlerAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
    {
        string toolName = request.Params.Name;

        if (_tools.TryGetValue(toolName, out McpTool? tool))
        {
            return tool.CallToolAsync(new() { Arguments = request.Params.Arguments?.AsReadOnly() }, cancellationToken);
        }

        return ValueTask.FromResult(new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = $"Unknown tool: '{toolName}'" }]
        });
    }

    private ValueTask<ListToolsResult> ListToolsHandlerAsync(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken)
    {
        List<Tool> tools = [];

        tools.AddRange(_tools.Select(tool => new Tool
        {
            Name = tool.Value.Name,
            Description = tool.Value.Description,
            InputSchema = tool.Value.GetInputSchema()
        }));

        return ValueTask.FromResult(new ListToolsResult { Tools = tools });
    }
}
