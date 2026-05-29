using System.CommandLine;
using System.CommandLine.Help;
using System.Text.Json.Nodes;
using UmbracoCompose.Cli.Utilities;

namespace UmbracoCompose.Cli.Commands;

internal abstract class BaseCommand : Command
{
    protected IConsole Console { get; }

    protected BaseCommand(string name, string description, IConsole console) : base(name, description)
    {
        Console = console;

        SetAction((Func<ParseResult, CancellationToken, Task<int>>)(async (parseResult, cancellationToken) =>
        {
            bool isJsonOutput = IsJsonOutput(parseResult);
            if (isJsonOutput)
            {
                Console.Output = ConsoleOutput.Error;
            }

            CommandResult result = await ExecuteAsync(parseResult, cancellationToken);

            if (result.ErrorMessage is not null)
            {
                if (isJsonOutput)
                {
                    await OutputJsonErrorAsync(result).ConfigureAwait(false);
                }
                else
                {
                    Console.DisplayError(result.ErrorMessage);
                }
            }

            if (result.ShouldDisplayHelp)
            {
                new HelpAction().Invoke(parseResult);
                return result.ExitCode;
            }

            return result.ExitCode;
        }));
    }

    private bool IsJsonOutput(ParseResult parseResult)
    {
        foreach (Option option in Options)
        {
            if (option.Name == "--format" && option is Option<OutputFormat> formatOption)
            {
                return parseResult.GetValue(formatOption) == OutputFormat.Json;
            }
        }
        return false;
    }

    private async Task OutputJsonErrorAsync(CommandResult result, bool writeIndented = false)
    {
        var errorObj = new JsonObject
        {
            ["error"] = new JsonObject
            {
                ["exitCode"] = result.ExitCode,
                ["message"] = result.ErrorMessage,
            }
        };

        if (!string.IsNullOrEmpty(result.ErrorResponse))
        {
            try
            {
                var parsed = JsonNode.Parse(result.ErrorResponse);
                errorObj["error"]!["response"] = parsed;
            }
            catch
            {
                errorObj["error"]!["response"] = result.ErrorResponse;
            }
        }

        var json = errorObj.ToJsonString(writeIndented ? JsonOutputHelper.Indented : JsonOutputHelper.Compact);
        Console.DisplayRawText(json);
    }

    protected abstract Task<CommandResult> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken);
}
