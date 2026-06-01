using System.CommandLine;

namespace Umbraco.Compose.Cli.Commands;

internal sealed class DiagnosticsCommand(IConsole console) : BaseCommand("diagnostics", "Diagnose your project", console)
{
    protected override Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
