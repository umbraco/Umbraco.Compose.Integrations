using Spectre.Console.Rendering;

namespace UmbracoCompose.Cli;

internal interface IConsole
{
    ConsoleOutput Output { get; set; }

    void DisplayError(string errorMessage);
    void DisplayMessage(Emoji emoji, string message, ConsoleOutput? consoleOverwrite = null);
    void DisplayRawText(string value, ConsoleOutput? consoleOverwrite = null);
    void DisplayRenderable(IRenderable content, ConsoleOutput? consoleOverwrite = null);
    Task<string?> ReadLineAsync(string prompt, bool masked = false, CancellationToken cancellationToken = default);
    Task<bool> ConfirmAsync(string prompt, bool defaultAnswer = false, CancellationToken cancellationToken = default);
}

internal enum ConsoleOutput
{
    Standard,
    Error,
}
