using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.JsonSchemaTypeHandlers;

internal sealed class ApiElementHandler : JsonSchemaTypeHandler<IApiElement>
{
    /// <inheritdoc />
    public override string GetTypeName(JsonSchemaGeneratorContext context, Type type) =>
        nameof(IApiElement);

    /// <inheritdoc />
    public override JsonSchema Handle(JsonSchemaGeneratorContext context, Type type)
    {
        const string discriminatorPropertyName = nameof(IApiElement.ContentType);

        JsonSchema schema = context.Generate<IApiElement>();
        schema.Discriminator = new()
        {
            PropertyName = context.Options.PropertyNamingPolicy?.ConvertName(discriminatorPropertyName) ?? discriminatorPropertyName
        };
        schema.Properties?.Remove("properties");
        return schema;
    }
}
