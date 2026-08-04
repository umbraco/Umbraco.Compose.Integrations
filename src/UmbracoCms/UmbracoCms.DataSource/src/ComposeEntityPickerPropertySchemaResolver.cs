using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.DataSource;

/// <summary>
/// Resolves JSON schema for Umbraco Compose entity picker property types.
/// </summary>
/// <param name="dataTypeService">The data type service.</param>
/// <param name="idKeyMap">The map used to resolve the data type's key from its published integer id.</param>
public sealed class ComposeEntityPickerPropertySchemaResolver(
    IDataTypeService dataTypeService,
    IIdKeyMap idKeyMap) : IPropertySchemaResolver
{
    /// <inheritdoc />
    public bool CanHandle(PublishedPropertyType propertyType) =>
        propertyType.EditorAlias.Equals(Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.EntityDataPicker) &&
            propertyType.DataType.ConfigurationObject is EntityDataPickerConfiguration configuration &&
            configuration.DataSource.Equals("Umbraco.Compose.PropertyEditorDataSource.Picker");

    /// <inheritdoc />
    public async Task<JsonSchema> ProcessAsync(JsonSchemaGeneratorContext context, PublishedPropertyType propertyType)
    {
        ArgumentNullException.ThrowIfNull(propertyType);
        ArgumentNullException.ThrowIfNull(context);

        int dataTypeId = propertyType.DataType.Id;

        Attempt<Guid> dataTypeKey = idKeyMap.GetKeyForId(dataTypeId, UmbracoObjectTypes.DataType);
        if (!dataTypeKey.Success)
        {
            throw new InvalidOperationException($"Could not resolve the key for data type '{dataTypeId}'.");
        }

        IDataType dataType = await dataTypeService.GetAsync(dataTypeKey.Result).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Could not get data type '{dataTypeId}'.");

        UmbracoComposeContentPickerDataSourceConfiguration configuration = new(dataType);

        return context
            .CreateBuilder(JsonPropertyType.Array)
            .Items(builder => builder.Type(JsonPropertyType.Object).Ref("https://umbracocompose.com/v1/node"))
            .CustomKeyword("$delivery", builder => builder.CustomKeyword("refCollection", configuration.Collection))
            .Build();
    }
}
