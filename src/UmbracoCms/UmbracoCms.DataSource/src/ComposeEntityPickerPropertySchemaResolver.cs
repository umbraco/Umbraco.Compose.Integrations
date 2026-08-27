using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
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
public sealed class ComposeEntityPickerPropertySchemaResolver : IPropertySchemaResolver
{
    private readonly IDataTypeService _dataTypeService;
    private readonly IIdKeyMap _idKeyMap;

    /// <summary>
    /// Initialises a ComposeEntityPickerPropertySchemaResolver.
    /// </summary>
    /// <param name="dataTypeService">The data type service.</param>
    /// <param name="idKeyMap">Map used to resolve the data type's key from its published integer id.</param>
    public ComposeEntityPickerPropertySchemaResolver(IDataTypeService dataTypeService, IIdKeyMap idKeyMap)
    {
        _dataTypeService = dataTypeService;
        _idKeyMap = idKeyMap;
    }

    /// <summary>
    /// Initialises a ComposeEntityPickerPropertySchemaResolver.
    /// </summary>
    /// <param name="dataTypeService">The data type service.</param>
    [Obsolete("Use the (IDataTypeService, IIdKeyMap) overload instead. This will be removed in a future update.")]
    public ComposeEntityPickerPropertySchemaResolver(IDataTypeService dataTypeService)
        : this(dataTypeService, StaticServiceProvider.Instance.GetRequiredService<IIdKeyMap>())
    {
    }

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

        Attempt<Guid> dataTypeKey = _idKeyMap.GetKeyForId(dataTypeId, UmbracoObjectTypes.DataType);
        if (!dataTypeKey.Success)
        {
            throw new InvalidOperationException($"Could not resolve the key for data type '{dataTypeId}'.");
        }

        IDataType dataType = await _dataTypeService.GetAsync(dataTypeKey.Result).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Could not get data type '{dataTypeId}'.");

        UmbracoComposeContentPickerDataSourceConfiguration configuration = new(dataType);

        return context
            .CreateBuilder(JsonPropertyType.Array)
            .Items(builder => builder.Type(JsonPropertyType.Object).Ref("https://umbracocompose.com/v1/node"))
            .CustomKeyword("$delivery", builder => builder.CustomKeyword("refCollection", configuration.Collection))
            .Build();
    }

    /// <inheritdoc />
    public JsonSchema Process(JsonSchemaGeneratorContext context, PublishedPropertyType propertyType) =>
        ProcessAsync(context, propertyType).GetAwaiter().GetResult();

}
