using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core;

/// <summary>
/// Defines the contract for resolving JSON schema information for Umbraco property types.
/// </summary>
public interface IPropertySchemaResolver
{
    /// <summary>
    /// Determines whether this resolver can handle the specified property type.
    /// </summary>
    /// <param name="propertyType">The property type to check.</param>
    /// <returns><c>true</c> if the resolver can handle the property type; otherwise, <c>false</c>.</returns>
    bool CanHandle(PublishedPropertyType propertyType);

    /// <summary>
    /// Processes the property type and generates its JSON schema.
    /// </summary>
    /// <param name="context">The JsonSchemaGeneratorContext providing access to registration methods and options.</param>
    /// <param name="propertyType">The property type to process.</param>
    /// <returns>The generated schema.</returns>
#pragma warning disable CS0618 // Type or member is obsolete
    ValueTask<JsonSchema> ProcessAsync(JsonSchemaGeneratorContext context, PublishedPropertyType propertyType) =>
        ValueTask.FromResult(Process(context, propertyType));
#pragma warning restore CS0618 // Type or member is obsolete

    /// <summary>
    /// Obsolete. Use <see cref="ProcessAsync(JsonSchemaGeneratorContext, PublishedPropertyType)"/> instead.
    /// Will be removed in a future version.
    /// </summary>
    /// <param name="context">The JsonSchemaGeneratorContext providing access to registration methods and options.</param>
    /// <param name="propertyType">The property type to process.</param>
    /// <returns>The generated schema.</returns>
    [Obsolete("Use the async method 'ProcessAsync' instead")]
    JsonSchema Process(JsonSchemaGeneratorContext context, PublishedPropertyType propertyType);
}
