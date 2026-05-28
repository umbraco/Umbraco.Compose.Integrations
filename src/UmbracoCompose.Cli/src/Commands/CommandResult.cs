namespace UmbracoCompose.Cli.Commands;

internal sealed class CommandResult
{
    public int ExitCode { get; }
    public string? ErrorMessage { get; }
    public bool ShouldDisplayHelp { get; }

    private CommandResult(int exitCode, string? errorMessage = null, bool shouldDisplayHelp = false)
    {
        ExitCode = exitCode;
        ErrorMessage = errorMessage;
        ShouldDisplayHelp = shouldDisplayHelp;
    }

    public static CommandResult Success() =>
        new(ExitCodes.Success);

    public static CommandResult Failure(int exitCode, string? errorMessage = null) =>
        new(exitCode, errorMessage);

    public static CommandResult DisplayHelp() =>
        new (ExitCodes.InvalidCommand, shouldDisplayHelp: true);
}
