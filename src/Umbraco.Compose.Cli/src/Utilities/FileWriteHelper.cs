using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Umbraco.Compose.Cli.Utilities;

internal sealed class FileWriteHelper
{
    public void WriteAtomic<T>(string targetPath, T value, JsonTypeInfo<T> jsonTypeInfo)
    {
        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(value, jsonTypeInfo);
        var tempPath = targetPath + ".tmp";
        try
        {
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, targetPath, overwrite: true);
        }
        catch (IOException)
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }

    public async Task<bool> WriteAtomicAsync(string filePath, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (directory is not null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, content, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, filePath, overwrite: true);
            return true;
        }
        catch
        {
            // Clean up temp file if it exists
            try
            {
                string tempPath = filePath + ".tmp";
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch { /* Ignore cleanup failures */ }

            return false;
        }
    }
}
