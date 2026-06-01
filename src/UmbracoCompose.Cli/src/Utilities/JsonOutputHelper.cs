using System.Text.Encodings.Web;
using System.Text.Json;

namespace UmbracoCompose.Cli.Utilities;

internal static class JsonOutputHelper
{
    public static readonly JsonSerializerOptions Default = new(AppJsonContext.Default.Options)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = AppJsonContext.Default,
        WriteIndented = true
    };

    public static readonly JsonSerializerOptions Indented = new(AppJsonContext.Default.Options)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = AppJsonContext.Default,
        WriteIndented = true
    };

    public static readonly JsonSerializerOptions Compact = new(AppJsonContext.Default.Options)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = AppJsonContext.Default,
        WriteIndented = false
    };
}
