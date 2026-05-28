using System.CommandLine;
using System.CommandLine.Help;

namespace UmbracoCompose.Cli.Commands;

internal abstract class BaseCommand : Command
{
    protected IConsole Console { get; }

    protected BaseCommand(string name, string description, IConsole console) : base(name, description)
    {
        Console = console;

        SetAction((Func<ParseResult, CancellationToken, Task<int>>)(async (parseResult, cancellationToken) =>
        {
            CommandResult result = await ExecuteAsync(parseResult, cancellationToken);

            if (result.ErrorMessage is not null)
            {
                Console.DisplayError(result.ErrorMessage);
            }

            if (result.ShouldDisplayHelp)
            {
                new HelpAction().Invoke(parseResult);
                return result.ExitCode;
            }

            return result.ExitCode;
        }));
    }

    protected abstract Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken);
}
