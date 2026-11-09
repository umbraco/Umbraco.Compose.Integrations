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
        JsonSchema schema = context.Generate<IApiMedia>();
        (schema.AllOf ??= []).Add(context.CreateBuilder().Ref("https://umbracocompose.com/v1/node").Build());
        schema.Required?.Remove("properties");
        schema.Properties?.Remove("properties");
        schema.Properties?["id"].Format = "id";
        return schema;
    }
}
