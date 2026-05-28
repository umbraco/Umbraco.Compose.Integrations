using System.CommandLine;
using System.CommandLine.Help;
using Microsoft.Extensions.Logging;
using BaseRootCommand = System.CommandLine.RootCommand;

namespace UmbracoCompose.Cli.Commands;

internal sealed class RootCommand : BaseRootCommand
{
    private static readonly Option<LogLevel?> s_logLevel = new("--log-level", "-l")
    {
        Description = "Set the minimum log level for console output",
        Recursive = true,
    };

    public RootCommand(
        AgentCommand agentCommand,
        DiagnosticsCommand diagnosticsCommand,
        GraphQLCommand graphQLCommand,
        IngestCommand ingestCommand,
        ManagementCommand managementCommand,
        ProfilesCommand profilesCommand
    ) : base("The Umbraco Compose CLI can be used to interact with the Umbraco Compose APIs.")
    {
        Action = new HelpAction();

        Subcommands.Add(agentCommand);
        // Subcommands.Add(managementCommand);
        // Subcommands.Add(diagnosticsCommand);
        // Subcommands.Add(ingestCommand);
        // Subcommands.Add(graphQLCommand);
        Subcommands.Add(profilesCommand);

        Options.Add(s_logLevel);
    }
}
