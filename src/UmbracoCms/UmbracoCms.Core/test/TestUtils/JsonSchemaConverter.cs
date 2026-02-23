using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Tests.Unit.TestUtils;

public sealed class JsonSchemaConverter : WriteOnlyJsonConverter<JsonSchema>
{
    public override void Write(VerifyJsonWriter writer, JsonSchema value)
    {
        writer.Serialize(System.Text.Json.JsonSerializer.SerializeToElement(value));
    }
}
