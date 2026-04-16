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
public sealed class ComposeEntityPickerPropertySchemaResolver(IDataTypeService dataTypeService) : IPropertySchemaResolver
{
    /// <inheritdoc />
    public bool CanHandle(PublishedPropertyType propertyType) =>
        propertyType.EditorAlias.Equals(Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.EntityDataPicker) &&
            propertyType.DataType.ConfigurationObject is EntityDataPickerConfiguration configuration &&
            configuration.DataSource.Equals("Umbraco.Compose.PropertyEditorDataSource.Picker");

    /// <inheritdoc />
    public JsonSchema? Process(PublishedPropertyType propertyType, JsonSchemaGenerator generator)
    {
        ArgumentNullException.ThrowIfNull(propertyType);
        ArgumentNullException.ThrowIfNull(generator);

#pragma warning disable CS0618 // 'IDataTypeService.GetDataType(int)' is obsolete: 'Please use GetAsync. Will be removed in V15.
        IDataType? dataType = dataTypeService.GetDataType(propertyType.DataType.Id);
#pragma warning restore CS0618
        if (dataType is null)
        {
            return null;
        }

        UmbracoComposeContentPickerDataSourceConfiguration configuration = new(dataType);

        return JsonSchemaBuilder
            .Create()
            .Type(JsonValueType.Array)
            .Items(builder => builder.Ref("https://umbracocompose.com/v1/node"))
            .CustomKeyword("$delivery", builder => builder.CustomKeyword("refCollection", configuration.Collection))
            .Build();
    }
}
