namespace Umbraco.Compose.Integrations.UmbracoCms.DataSource;

/// <summary>
/// The result of a content query, including the items retrieved, any error message, and paging information.
/// </summary>
public sealed class ContentQueryResult
{
    /// <summary>
    /// Indicates whether the content query was successful - and indirectly if we have items, or an error message.
    /// </summary>
    public bool Success => Items is not null && ErrorMessage is null;

    /// <summary>
    /// In the case of <see cref="Success"/> being false, this contains the error message.
    /// </summary>
    public string? ErrorMessage { get; set; } = default;

    /// <summary>
    /// In the case of <see cref="Success"/> being true, this contains the found items.
    /// </summary>
    public IEnumerable<object>? Items { get; set; } = default;

    /// <summary>
    /// The paging information for the query result.
    /// </summary>
    public ContentQueryPaging? Paging { get; set; } = default;


    private ContentQueryResult(string? errorMessage, IEnumerable<object>? items, ContentQueryPaging? paging)
    {
        ErrorMessage = errorMessage;
        Items = items;
        Paging = paging;
    }

    /// <summary>
    /// Returns a ContentQueryResult representing a successful query with result itemss.
    /// </summary>
    /// <param name="items">The results of the query</param>
    /// <param name="paging">The paging information</param>
    /// <returns>A ContentQueryResult with Success = true</returns>
    public static ContentQueryResult Ok(IEnumerable<object> items, ContentQueryPaging paging)
    {
        return new (null, items, paging);
    }

    /// <summary>
    /// Returns a ContentQueryResult representing an unsuccessful query with an error message.
    /// </summary>
    /// <param name="errorMesssaage">The error mesage descrribing the reason for failure</param>
    /// <returns>A ContentQueryResult with Success = fale</returns>
    public static ContentQueryResult Error (string errorMesssaage)
    {
        return new (errorMesssaage, null, null);
    }
}
