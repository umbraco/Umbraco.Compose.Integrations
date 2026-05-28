namespace UmbracoCompose.Cli;

internal interface IConsole
{
    void DisplayError(string errorMessage);
    void DisplayMessage(Emoji emoji, string message);
    Task<string?> ReadLineAsync(string prompt, bool masked = false, CancellationToken cancellationToken = default);
}
