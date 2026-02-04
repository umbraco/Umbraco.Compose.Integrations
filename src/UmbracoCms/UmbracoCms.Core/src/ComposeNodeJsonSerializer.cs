using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core;

internal sealed class ComposeNodeJsonSerializer : JsonConverter<ComposeNode>
{
    public override ComposeNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() is string value ? new(value) : null;

    public override void Write(Utf8JsonWriter writer, ComposeNode value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Id);
}
