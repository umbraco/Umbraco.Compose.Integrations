using Spectre.Console;
using Spectre.Console.Rendering;

namespace Umbraco.Compose.Cli;

internal interface IConsole
{
    ConsoleOutput Output { get; set; }
    bool IsOutputRedirected { get; }

    void DisplayError(string errorMessage);
    void DisplayMessage(Emoji emoji, string message, ConsoleOutput? consoleOverwrite = null);
    void DisplayRawText(string value, ConsoleOutput? consoleOverwrite = null);
    void DisplayRenderable(IRenderable content, ConsoleOutput? consoleOverwrite = null);
    Task<string?> ReadLineAsync(string prompt, bool masked = false, CancellationToken cancellationToken = default);
    Task<bool> ConfirmAsync(string prompt, bool defaultAnswer = false, CancellationToken cancellationToken = default);
    Task<T[]> MultiSelectPromptAsync<T>(MultiSelectionPrompt<T> prompt, CancellationToken cancellationToken = default) where T : notnull;
}

internal enum ConsoleOutput
{
    Standard,
    Error,
}
