using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using Umbraco.Compose.Cli;
using Umbraco.Compose.Cli.Clients;
using Umbraco.Compose.Cli.Commands;
using Umbraco.Compose.Cli.Services;
using Umbraco.Compose.Cli.Utilities;
using RootCommand = Umbraco.Compose.Cli.Commands.RootCommand;

HostApplicationBuilderSettings settings = new()
{
    Configuration = new(),
};
settings.Configuration.AddEnvironmentVariables();

LoggingOptions loggingOptions = ParseLoggingOptions(args);
ILoggerFactory loggerFactory = CreateLoggerFactory(loggingOptions);
ILogger rootLogger = loggerFactory.CreateLogger("Umbraco.Compose.Cli");

HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings);

builder.Services.AddSingleton(loggingOptions);
builder.Services.AddSingleton(loggerFactory);

builder.Services.AddTransient<AgentCommand>();
builder.Services.AddTransient<AgentInitCommand>();
builder.Services.AddTransient<AgentMcpCommand>();
builder.Services.AddTransient<FileWriteHelper>();
builder.Services.AddTransient<DiagnosticsCommand>();
builder.Services.AddTransient<GraphQLCommand>();
builder.Services.AddTransient<GraphQlIntrospectCommand>();
builder.Services.AddTransient<GraphQlQueryCommand>();
builder.Services.AddTransient<IngestCommand>();
builder.Services.AddTransient<ManagementCommand>();
builder.Services.AddTransient<ProfilesCommand>();
builder.Services.AddTransient<ProfileAddCommand>();
builder.Services.AddTransient<ProfileListCommand>();
builder.Services.AddTransient<ProfileRemoveCommand>();
builder.Services.AddTransient<ProfileSetDefaultCommand>();
builder.Services.AddTransient<ProfileShowCommand>();
builder.Services.AddTransient<RootCommand>();
builder.Services.ConfigureHttpClientDefaults(httpClientBuilder =>
{
    httpClientBuilder.AddStandardResilienceHandler();
    httpClientBuilder.ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("umbraco-compose-cli", "1.0.0"));
    });
});
builder.Services.AddTransient<IOAuthService, OAuthService>();
builder.Services.AddTransient<ProfileValidateCommand>();

builder.Services.AddSingleton<GraphQLRequestExecutor>();
builder.Services.AddSingleton<IConsole, SpectreConsole>();
builder.Services.AddSingleton<ProfileConfigService>();
builder.Services.AddTransient<ProfileResolver>();
builder.Services.AddScoped<VariableParser>();
builder.Services.AddScoped<ResponseFormatter>();
builder.Services.AddSingleton<IngestionService>();
builder.Services.AddSingleton<IngestionApiClientFactory>();

using IHost app = builder.Build();
await app.StartAsync().ConfigureAwait(false);

var rootCommand = app.Services.GetRequiredService<RootCommand>();
var parseResult = rootCommand.Parse(args);

int exitCode = await parseResult.InvokeAsync().ConfigureAwait(false);

rootLogger.LogInformation("Exit Code {ExitCode}", exitCode);

return exitCode;

ILoggerFactory CreateLoggerFactory(LoggingOptions loggingOptions)
{
    bool isMcpStartCommand = args.Length >= 2 &&
        args[0] == "agent" && args[1] == "mcp";

    return LoggerFactory.Create(builder =>
    {
        if (loggingOptions.LogLevel.HasValue)
        {
            builder.AddFilter("Umbraco.Compose.Cli", loggingOptions.LogLevel.Value);
            builder.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
        }
        if (isMcpStartCommand)
        {
            builder.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });
        }
        else if (loggingOptions.LogLevel.HasValue)
        {
            builder.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = loggingOptions.LogLevel.Value;
            });
        }
    });
}

LoggingOptions ParseLoggingOptions(string[] args)
{
    LogLevel? logLevel = null;

    bool debugMode = false;

    if (args is not null && args.Length > 0)
    {
        debugMode = args.Any(x => x == "--debug");

        for (var i = 0; i < args.Length; ++i)
        {
            if ((args[i] == "--log-level" || args[i] == "-l") && i + 1 < args.Length)
            {
                if (Enum.TryParse<LogLevel>(args[i + 1], ignoreCase: true, out var level))
                {
                    logLevel = level;
                }
                break;
            }
        }

        if (debugMode && logLevel is null)
        {
            logLevel = LogLevel.Debug;
        }
    }

    return new(logLevel, debugMode);
}

internal sealed record LoggingOptions(LogLevel? LogLevel, bool Debug);
