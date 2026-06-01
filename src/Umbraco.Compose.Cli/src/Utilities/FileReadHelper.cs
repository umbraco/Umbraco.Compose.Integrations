using Umbraco.Compose.Cli.Commands;

namespace Umbraco.Compose.Cli.Utilities;

internal static class FileReadHelper
{
    private const string FileReadPrefix = "@";

    public static async Task<(string? Content, CommandResult? Error)> ReadFromFileOrReturnAsync(string value, CancellationToken cancellationToken)
    {
        if (value.StartsWith(FileReadPrefix, StringComparison.Ordinal))
        {
            var filePath = value.Substring(FileReadPrefix.Length);
            if (!File.Exists(filePath))
            {
                return (null, CommandResult.Failure(ExitCodes.ValidationError, $"File not found: {filePath}"));
            }
            try
            {
                var content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
                return (content, null);
            }
            catch (IOException ex)
            {
                return (null, CommandResult.Failure(ExitCodes.ValidationError, $"Failed to read file '{filePath}': {ex.Message}"));
            }
        }
        return (value, null);
    }
}
