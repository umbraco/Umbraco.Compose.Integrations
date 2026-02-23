namespace Umbraco.Compose.Integrations.UmbracoCms.DataSource;

/// <summary>
/// Represents the requested paging for a content query.
/// </summary>
/// <param name="After">Take items after, not including, the provided cursor. This cursor is provided by previous queries "EndCursor".</param>
/// <param name="Take">The number of items to take.</param>
public sealed record UmbracoComposeContentPickerDataSourcePaging(string? After, int Take);
