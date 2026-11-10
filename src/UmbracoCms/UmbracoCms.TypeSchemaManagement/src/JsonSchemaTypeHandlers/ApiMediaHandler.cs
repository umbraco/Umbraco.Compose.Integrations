using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.JsonSchemaTypeHandlers;

internal sealed class ApiMediaHandler : JsonSchemaTypeHandler<IApiMedia>
{
    /// <inheritdoc />
    public override string GetTypeName(JsonSchemaGeneratorContext context, Type type) =>
        nameof(IApiMedia);

    /// <inheritdoc />
    public override JsonSchema Handle(JsonSchemaGeneratorContext context, Type type)
    {
        const string discriminatorPropertyName = nameof(IApiMedia.MediaType);

        JsonSchema schema = context.Generate<IApiMedia>();
        schema.Discriminator = new()
        {
            PropertyName = context.Options.PropertyNamingPolicy?.ConvertName(discriminatorPropertyName) ?? discriminatorPropertyName
        };
        schema.Required?.Remove("properties");
        schema.Properties?.Remove("properties");
        return schema;
    }
}
