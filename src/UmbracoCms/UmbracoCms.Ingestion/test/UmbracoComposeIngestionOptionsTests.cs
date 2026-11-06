namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Tests.Unit;

public sealed class UmbracoComposeIngestionOptionsTests
{
    [Fact]
    public void IsValid_False_WhenNothingConfigured()
    {
        UmbracoComposeIngestionOptions options = new();

        Assert.False(options.IsValid);
    }

    [Fact]
    public void IsValid_True_WhenCollectionAliasSet()
    {
        UmbracoComposeIngestionOptions options = new() { CollectionAlias = "content" };

        Assert.True(options.IsValid);
    }

    [Fact]
    public void IsValid_True_WhenCollectionsConfigured()
    {
        UmbracoComposeIngestionOptions options = new()
        {
            Collections = new()
            {
                Content = "content",
                Media = "media"
            }
        };

        Assert.True(options.IsValid);
    }

    [Fact]
    public void Collections_IsValid_False_WhenNotConfigured()
    {
        UmbracoComposeIngestionCollectionsOptions collections = new();

        Assert.False(collections.IsValid);
    }

    [Fact]
    public void Collections_IsValid_False_WhenOnlyContentConfigured()
    {
        UmbracoComposeIngestionCollectionsOptions collections = new() { Content = "content" };

        Assert.False(collections.IsValid);
    }

    [Fact]
    public void Collections_IsValid_True_WhenConfigured()
    {
        UmbracoComposeIngestionCollectionsOptions collections = new()
        {
            Content = "content",
            Media = "media"
        };

        Assert.True(collections.IsValid);
    }

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
