using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace UmbracoCompose.Cli.Utilities;

internal static class FileWriteHelper
{
    public static void WriteAtomic<T>(string targetPath, T value, JsonTypeInfo<T> jsonTypeInfo)
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
}
