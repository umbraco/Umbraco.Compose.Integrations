using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UmbracoCompose.Cli;
using UmbracoCompose.Cli.Commands;
using RootCommand = UmbracoCompose.Cli.Commands.RootCommand;

HostApplicationBuilderSettings settings = new()
{
    Configuration = new(),
};
settings.Configuration.AddEnvironmentVariables();

ILoggerFactory loggerFactory = CreateLoggerFactory(args);
ILogger rootLogger = loggerFactory.CreateLogger("UmbracoCompose.Cli");

HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings);

builder.Services.AddSingleton(loggerFactory);

builder.Services.AddTransient<AgentCommand>();
builder.Services.AddTransient<AgentMcpCommand>();
builder.Services.AddTransient<DiagnosticsCommand>();
builder.Services.AddTransient<GraphQLCommand>();
builder.Services.AddTransient<IngestCommand>();
builder.Services.AddTransient<ManagementCommand>();
builder.Services.AddTransient<ProfilesCommand>();
builder.Services.AddTransient<ProfileAddCommand>();
builder.Services.AddTransient<ProfileListCommand>();
builder.Services.AddTransient<ProfileRemoveCommand>();
builder.Services.AddTransient<ProfileSetDefaultCommand>();
builder.Services.AddTransient<ProfileShowCommand>();
builder.Services.AddTransient<RootCommand>();

builder.Services.AddSingleton<IConsole, SpectreConsole>();

using IHost app = builder.Build();
await app.StartAsync().ConfigureAwait(false);

var rootCommand = app.Services.GetRequiredService<RootCommand>();
var parseResult = rootCommand.Parse(args);

int exitCode = await parseResult.InvokeAsync().ConfigureAwait(false);

rootLogger.LogInformation("Exit Code {ExitCode}", exitCode);

return exitCode;

ILoggerFactory CreateLoggerFactory(string[] args)
{
    bool isMcpStartCommand = args.Length >= 2 &&
        args[0] == "agent" && args[1] == "mcp";

    LogLevel? logLevel = null;

    if (args is not null && args.Length > 0)
    {
        for (var i = 0; i < args.Length; ++i)
        {
            if ((args[i] == "--log-level" || args[i] == "-l") && args.Length >= i+1)
            {
                if (Enum.TryParse<LogLevel>(args[i + 1], ignoreCase: true, out var level))
                {
                    logLevel = level;
                }
                break;
            }
        }
    }

    return LoggerFactory.Create(builder =>
    {
        if (isMcpStartCommand)
        {
            builder.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });
        }
        else if (logLevel.HasValue)
        {
            builder.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = logLevel.Value;
            });
        }
    });
}
