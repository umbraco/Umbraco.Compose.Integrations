using Spectre.Console;
using Spectre.Console.Rendering;

namespace UmbracoCompose.Cli;

internal sealed class SpectreConsole : IConsole
{
    private readonly IAnsiConsole _error;
    private readonly IAnsiConsole _out;

    public ConsoleOutput Output { get; set; } = ConsoleOutput.Standard;

    public SpectreConsole()
    {
        _error = AnsiConsole.Create(new()
        {
            Ansi = AnsiSupport.Detect,
            ColorSystem = ColorSystemSupport.Detect,
            Interactive = Console.IsInputRedirected ? InteractionSupport.No : InteractionSupport.Detect,
            Out = new AnsiConsoleOutput(Console.Error),
        });

        _out = AnsiConsole.Create(new()
        {
            Ansi = AnsiSupport.Detect,
            ColorSystem = ColorSystemSupport.Detect,
            Interactive = Console.IsInputRedirected ? InteractionSupport.No : InteractionSupport.Detect,
            Out = new AnsiConsoleOutput(Console.Out),
        });
    }

    public void DisplayError(string errorMessage) =>
        WriteMessage(_error, Emojis.CrossMark, $"[red bold]{errorMessage}[/]");

    public void DisplayMessage(Emoji emoji, string message, ConsoleOutput? consoleOverwrite = null) =>
        WriteMessage(GetConsole(consoleOverwrite), emoji, message);

    public void DisplayRawText(string value, ConsoleOutput? consoleOverwrite = null) =>
        GetConsole(consoleOverwrite).WriteLine(value);

    public void DisplayRenderable(IRenderable content, ConsoleOutput? consoleOverwrite = null) =>
        GetConsole(consoleOverwrite).Write(content);

    public async Task<string?> ReadLineAsync(string prompt, bool masked = false, CancellationToken cancellationToken = default)
    {
        TextPrompt<string> textPrompt = new (prompt);

        if (masked)
        {
            textPrompt = textPrompt.Secret();
        }

        return await _out.PromptAsync(textPrompt, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> ConfirmAsync(string prompt, bool defaultAnswer = false, CancellationToken cancellationToken = default) =>
        _out.ConfirmAsync(prompt, defaultAnswer, cancellationToken: cancellationToken);

    private static void WriteMessage(IAnsiConsole console, Emoji emoji, string message)
    {
        Grid grid = new();
        grid.AddColumn();
        grid.AddColumn();
        grid.Columns[0].NoWrap = true;
        grid.Columns[0].Padding = new (0);
        grid.Columns[1].Padding = new (0);

        grid.AddRow(
            new Markup(emoji),
            new Markup(message)
        );

        console.Write(grid);
    }

    private IAnsiConsole GetConsole(ConsoleOutput? consoleOverwrite) => (consoleOverwrite ?? Output) switch
    {
        ConsoleOutput.Error => _error,
        _ => _out
    };
}
