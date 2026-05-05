using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PropertyEditors.DeliveryApi;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;

namespace Umbraco.Compose.Integrations.UmbracoCms.DataSource;

///<summary>
/// Value Converter for the Entity Data Picker property editor with Compose Picker Data Source
/// </summary>
/// <param name="coreConverter">The Core Entity Data Picker Value Converter</param>
public sealed class EntityDataPickerDeliveryValueConverter(EntityDataPickerValueConverter coreConverter)
    : PropertyValueConverterBase, IDeliveryApiPropertyValueConverter
{
    private const string DataSourceAlias = "Umbraco.Compose.PropertyEditorDataSource.Picker";

    /// <inheritdoc />
    public override bool IsConverter(IPublishedPropertyType propertyType) =>
        coreConverter.IsConverter(propertyType) &&
            propertyType.DataType.ConfigurationObject is EntityDataPickerConfiguration config &&
            DataSourceAlias.Equals(config.DataSource);

    /// <inheritdoc />
    public override Type GetPropertyValueType(IPublishedPropertyType propertyType) =>
        coreConverter.GetPropertyValueType(propertyType);

    /// <inheritdoc />
    public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType) =>
        coreConverter.GetPropertyCacheLevel(propertyType);

    /// <inheritdoc />
    public PropertyCacheLevel GetDeliveryApiPropertyCacheLevel(IPublishedPropertyType propertyType) =>
        PropertyCacheLevel.Element;

    /// <inheritdoc />
    public override object? ConvertSourceToIntermediate(
        IPublishedElement owner,
        IPublishedPropertyType propertyType,
        object? source,
        bool preview) =>
        coreConverter.ConvertSourceToIntermediate(owner, propertyType, source, preview);

    /// <inheritdoc />
    public override object? ConvertIntermediateToObject(
        IPublishedElement owner,
        IPublishedPropertyType propertyType,
        PropertyCacheLevel referenceCacheLevel,
        object? inter,
        bool preview) =>
        coreConverter.ConvertIntermediateToObject(owner, propertyType, referenceCacheLevel, inter, preview);

    /// <inheritdoc />
    public object? ConvertIntermediateToDeliveryApiObject(
        IPublishedElement owner,
        IPublishedPropertyType propertyType,
        PropertyCacheLevel referenceCacheLevel,
        object? inter,
        bool preview,
        bool expanding)
    {
        if (inter is EntityDataPickerValue value)
        {
            return value.Ids;
        }

        return inter;
    }

    /// <inheritdoc />
    public Type GetDeliveryApiPropertyValueType(IPublishedPropertyType propertyType) =>
        typeof(IEnumerable<string>);
}
