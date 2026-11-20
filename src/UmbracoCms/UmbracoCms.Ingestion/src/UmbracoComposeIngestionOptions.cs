namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Options for Umbraco Compose ingestion.
/// </summary>
public sealed class UmbracoComposeIngestionOptions
{
    private readonly Dictionary<Type, Func<UmbracoComposeIngestionOptions, string>> _collectionAliasMap = [];

    /// <summary>
    /// The collection alias to ingest content into.
    /// </summary>
    [Obsolete("This will be removed in v20, use the individual configuration in Collections instead.")]
    public string CollectionAlias { get; set; } = default!;

    ///<summary>
    /// Collections options.
    /// </summary>
    public UmbracoComposeIngestionCollectionsOptions Collections { get; set; } = new();

    ///<summary>
    /// Whether the options are valid.
    /// </summary>
    [Obsolete("This will be removed in v20.")]
    public bool IsValid =>
        CollectionAlias is not null;

    /// <summary>
    /// Registers a method that for given a <see cref="IngestQueueItem"/> returns a collection alias.
    /// </summary>
    /// <typeparam name="T">The <see langword="type"/> to register the resolver for.</typeparam>
    /// <param name="resolver">The resolver method.</param>
    /// <returns>The current instance of <see cref="UmbracoComposeIngestionOptions" />.</returns>
    public UmbracoComposeIngestionOptions RegisterCollectionAliasResolver<T>(
        Func<UmbracoComposeIngestionOptions, string> resolver) where T : IngestQueueItem
    {
        ArgumentNullException.ThrowIfNull(resolver);

        _collectionAliasMap[typeof(T)] = resolver;
        return this;
    }

    /// <summary>
    /// Gets the collection alias for the given a <see cref="IngestQueueItem"/>.
    /// </summary>
    /// <typeparam name="T">The type to get the collection alias for.</typeparam>
    /// <returns>
    /// The collection alias or <c>null</c> if no resolver is registered for the <see langword="type"/>.
    /// </returns>
    public string? GetCollectionAlias<T>() where T : IngestQueueItem =>
        GetCollectionAlias(typeof(T));

    internal string? GetCollectionAlias(Type type) =>
        _collectionAliasMap.TryGetValue(type, out Func<UmbracoComposeIngestionOptions, string>? resolver) ?
            resolver(this) :
            null;
}
