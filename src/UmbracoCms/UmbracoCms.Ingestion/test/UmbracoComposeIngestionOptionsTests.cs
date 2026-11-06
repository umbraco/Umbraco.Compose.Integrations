namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Tests.Unit;

public sealed class UmbracoComposeIngestionOptionsTests
{
    [Fact]
    public void GetCollectionAlias_ReturnsRegisteredResolverValue()
    {
        UmbracoComposeIngestionOptions options = new()
        {
            Collections = new()
            {
                Content = "content",
                Media = "media"
            }
        };

        options
            .RegisterCollectionAliasResolver<ContentIngestQueueItem>(o => o.Collections.Content ?? o.CollectionAlias)
            .RegisterCollectionAliasResolver<MediaIngestQueueItem>(o => o.Collections.Media);

        Assert.Equal("content", options.GetCollectionAlias(typeof(ContentIngestQueueItem)));
        Assert.Equal("media", options.GetCollectionAlias(typeof(MediaIngestQueueItem)));
    }

    [Fact]
    public void GetCollectionAlias_ReturnsNull_WhenNoResolverRegistered()
    {
        UmbracoComposeIngestionOptions options = new();

        Assert.Null(options.GetCollectionAlias(typeof(ContentIngestQueueItem)));
    }

    [Fact]
    public void RegisterCollectionAliasResolver_ReturnsSameInstance()
    {
        UmbracoComposeIngestionOptions options = new();

        UmbracoComposeIngestionOptions result = options.RegisterCollectionAliasResolver<ContentIngestQueueItem>(_ => "content");

        Assert.Same(options, result);
    }
}
