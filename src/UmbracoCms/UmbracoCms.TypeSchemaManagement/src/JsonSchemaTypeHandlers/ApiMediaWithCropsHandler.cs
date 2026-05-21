using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.JsonSchemaTypeHandlers;

internal sealed class ApiMediaWithCropsHandler : JsonSchemaTypeHandler<IApiMediaWithCrops>
{
    /// <inheritdoc />
    public override string GetTypeName(JsonSchemaGeneratorContext context, Type type) =>
        nameof(ApiMediaWithCrops);

    /// <inheritdoc />
    public override JsonSchema Handle(JsonSchemaGeneratorContext context, Type type) =>
        context.Generate<ApiMediaWithCrops>();

#pragma warning disable S1144,S2325  // Remove the unused private property, Make property static
    // type only used for schema generation using
    private sealed class ApiMediaWithCrops
    {
        public ImageFocalPoint? FocalPoint =>
            throw new NotImplementedException();

        public IEnumerable<ImageCrop>? Crops =>
            throw new NotImplementedException();

        public string Name =>
            throw new NotImplementedException();

        public string MediaType =>
            throw new NotImplementedException();

        public string Url =>
            throw new NotImplementedException();

        public string? Extension =>
            throw new NotImplementedException();

        public int? Width =>
            throw new NotImplementedException();

        public int? Height =>
            throw new NotImplementedException();

        public int? Bytes =>
            throw new NotImplementedException();
    }
#pragma warning restore S1144, S2325
}
