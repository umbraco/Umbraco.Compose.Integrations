using Spectre.Console;

namespace UmbracoCompose.Cli;

internal sealed class SpectreConsole : IConsole
{
    private readonly IAnsiConsole _error;
    private readonly IAnsiConsole _out;

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

    public void DisplayMessage(Emoji emoji, string message) =>
        WriteMessage(_out, emoji, message);

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
}
