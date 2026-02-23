using System.Text.Json.Serialization;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

[JsonSerializable(typeof(TypeSchemaDto))]
[JsonSerializable(typeof(List<TypeSchemaDto>))]
internal sealed partial class TypeSchemaJsonSerializerContext : JsonSerializerContext;
