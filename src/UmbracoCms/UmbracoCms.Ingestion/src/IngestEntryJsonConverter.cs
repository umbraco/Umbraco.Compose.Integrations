using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

internal sealed class IngestEntryJsonConverter : JsonConverter<IngestEntry>
{
    public override IngestEntry? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, IngestEntry value, JsonSerializerOptions options)
    {
        // ensures we serialize the implementation and not just base class
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
