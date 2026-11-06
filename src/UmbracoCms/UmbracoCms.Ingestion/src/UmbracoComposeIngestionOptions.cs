namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Options for Umbraco Compose ingestion.
/// </summary>
public sealed class UmbracoComposeIngestionOptions
{
    private readonly Dictionary<Type, Func<UmbracoComposeIngestionOptions, string>> _collectionAliasMap = [];

    /// <summary>
    /// The collection alias to ingest content into,
    /// </summary>
    [Obsolete("This will be removed in v20, use the individual configuration in Collectins instead.")]
    public string CollectionAlias { get; set; } = default!;

    ///<summary>
    /// Collections options.
    /// </summary>
    public UmbracoComposeIngestionCollectionsOptions Collections { get; set; } = new();

    ///<summary>
    /// Whether the options are valid.
    /// </summary>
    public bool IsValid =>
        CollectionAlias is not null || Collections.IsValid;

    /// <summary>
    /// Registers a method that for given a <see cref="IngestQueueItem"/> returns a collection alias.
    /// </summary>
    /// <typeparam name="T">The type to register the resolver for.</typeparam>
    /// <param name="resolver">The resolver method</param>
    /// <returns>The current instance of <see cref="UmbracoComposeIngestionOptions" />.</returns>
    public UmbracoComposeIngestionOptions RegisterCollectionAliasResolver<T>(
        Func<UmbracoComposeIngestionOptions, string> resolver) where T : IngestQueueItem
    {
        _collectionAliasMap[typeof(T)] = resolver;
        return this;
    }

    internal string? GetCollectionAlias(Type type) =>
        _collectionAliasMap.TryGetValue(type, out Func<UmbracoComposeIngestionOptions, string>? resolver) ?
            resolver(this) :
            null;
}
