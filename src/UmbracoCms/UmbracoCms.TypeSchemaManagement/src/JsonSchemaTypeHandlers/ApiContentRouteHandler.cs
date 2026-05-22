using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.JsonSchemaTypeHandlers;

internal sealed class ApiContentRouteHandler : JsonSchemaTypeHandler<IApiContentRoute>
{
    /// <inheritdoc />
    public override string GetTypeName(JsonSchemaGeneratorContext context, Type type) =>
        nameof(ApiContentRoute);

    /// <inheritdoc />
    public override JsonSchema Handle(JsonSchemaGeneratorContext context, Type type) =>
        context.Generate<ApiContentRoute>();
}
