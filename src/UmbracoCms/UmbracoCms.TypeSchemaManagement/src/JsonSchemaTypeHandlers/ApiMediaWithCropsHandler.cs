using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;
using Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.JsonSchemaTypeHandlers;

internal sealed class ApiMediaWithCropsHandler(
    IOptions<UmbracoComposeIngestionOptions> ingestionOptions) : JsonSchemaTypeHandler<IApiMediaWithCrops>
{
    /// <inheritdoc />
    public override string GetTypeName(JsonSchemaGeneratorContext context, Type type) =>
        "ApiMediaWithCrops";

    /// <inheritdoc />
    public override JsonSchema Handle(JsonSchemaGeneratorContext context, Type type)
    {
        if (!context.TryGetSchema(typeof(ImageFocalPoint), out JsonSchema? focalPointSchema))
        {
            focalPointSchema = context.Generate<ImageFocalPoint>();
        }

        if (!context.TryGetSchema(typeof(ImageCrop), out JsonSchema? imageCropSchema))
        {
            imageCropSchema = context.Generate<ImageCrop>();
        }

        JsonSchemaBuilder builder = context.CreateBuilder(JsonPropertyType.Object)
            .Property(
                "id",
                builder =>
                    builder.Type(JsonPropertyType.Object)
                        .Ref("IApiMedia")
                        .CustomKeyword(
                            "$delivery",
                            builder =>
                                builder.CustomKeyword(
                                    "refCollection",
                                    ingestionOptions.Value.GetCollectionAlias<MediaIngestQueueItem>())
                                    .CustomKeyword("fieldName", "media")))
            .Property("focalPoint", builder => builder.Ref(focalPointSchema.TypeName ?? nameof(ImageFocalPoint)))
            .Property(
                "crops",
                builder => builder.Type(JsonPropertyType.Array)
                    .Items(builder => builder.Ref(imageCropSchema.TypeName ?? nameof(ImageCrop))));

        return builder.Build();
    }
}
