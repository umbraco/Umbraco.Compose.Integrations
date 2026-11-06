using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Models.DeliveryApi;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Tests.Unit.MediaIngestion;

public sealed class MediaIngestQueueItemProcessorTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization { ConfigureMembers = true });

    private IPublishedMediaCache _mediaCache = null!;
    private IApiMediaBuilder _apiMediaBuilder = null!;
    private IMediaNavigationQueryService _navigationQueryService = null!;
    private MediaIngestQueueItemProcessor _processor = null!;

    [Fact]
    public async Task DeletePayload_YieldsDeleteAndDeleteWhereEntries()
    {
        Guid id = Guid.NewGuid();
        SetupProcessor();

        List<IngestEntry> entries = await ProcessAsync(new MediaIngestQueueItem(new[] { new MediaChangePayload(id, MediaChangeType.Delete) }), TestContext.Current.CancellationToken);

        Assert.Equal(2, entries.Count);

        DeleteEntry deleteEntry = Assert.IsType<DeleteEntry>(entries[0]);
        Assert.Equal("delete", deleteEntry.Action);
        Assert.Equal(id.ToString(), deleteEntry.Id);

        DeleteWhereEntry deleteWhereEntry = Assert.IsType<DeleteWhereEntry>(entries[1]);
        Assert.Equal("delete", deleteWhereEntry.Action);
        Assert.Equal(new[] { id.ToString() }, Assert.IsType<string[]>(deleteWhereEntry.Where["ancestors_some"]));

        A.CallTo(() => _mediaCache.GetByIdAsync(id)).MustNotHaveHappened();
        A.CallTo(() => _apiMediaBuilder.Build(A<IPublishedContent>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task MediaNotFound_YieldsNoEntries()
    {
        Guid id = Guid.NewGuid();
        SetupProcessor();
        A.CallTo(() => _mediaCache.GetByIdAsync(id)).Returns((IPublishedContent?)null);

        List<IngestEntry> entries = await ProcessAsync(new MediaIngestQueueItem(new[] { new MediaChangePayload(id, MediaChangeType.Update) }), TestContext.Current.CancellationToken);

        Assert.Empty(entries);
        A.CallTo(() => _apiMediaBuilder.Build(A<IPublishedContent>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ApiMediaNotBuilt_YieldsNoEntries()
    {
        Guid id = Guid.NewGuid();
        SetupProcessor();

        IPublishedContent media = CreatePublishedMedia(id, "umbracoImage");
        A.CallTo(() => _mediaCache.GetByIdAsync(id)).Returns(media);
        A.CallTo(() => _apiMediaBuilder.Build(media)).Returns(null!);

        List<IngestEntry> entries = await ProcessAsync(new MediaIngestQueueItem(new[] { new MediaChangePayload(id, MediaChangeType.Update) }), TestContext.Current.CancellationToken);

        Assert.Empty(entries);
    }

    [Fact]
    public async Task MediaFound_YieldsUpsertEntry()
    {
        Guid id = Guid.NewGuid();
        Guid parentId = Guid.NewGuid();
        Guid ancestor1 = Guid.NewGuid();
        Guid ancestor2 = Guid.NewGuid();
        SetupProcessor();

        IPublishedContent media = CreatePublishedMedia(id, "umbracoImage");
        A.CallTo(() => _mediaCache.GetByIdAsync(id)).Returns(media);

        IApiMedia apiMedia = CreateApiMedia(id, "umbracoImage");
        A.CallTo(() => _apiMediaBuilder.Build(media)).Returns(apiMedia);
        Guid? outParentId = null;
        A.CallTo(() => _navigationQueryService.TryGetParentKey(id, out outParentId))
            .AssignsOutAndRefParameters(parentId);
        IEnumerable<Guid> outAncestors = null!;
        A.CallTo(() => _navigationQueryService.TryGetAncestorsKeys(id, out outAncestors))
            .Returns(true)
            .AssignsOutAndRefParameters(new[] { ancestor1, ancestor2 });

        List<IngestEntry> entries = await ProcessAsync(new MediaIngestQueueItem(new[] { new MediaChangePayload(id, MediaChangeType.Update) }), TestContext.Current.CancellationToken);

        UpsertMediaEntry upsertEntry = Assert.IsType<UpsertMediaEntry>(Assert.Single(entries));
        Assert.Equal("upsert", upsertEntry.Action);
        Assert.Equal(id.ToString(), upsertEntry.Id);
        Assert.Equal("umbracoImage", upsertEntry.Type);

        ComposeApiMedia data = upsertEntry.Data;
        Assert.Equal(id, data.Id);
        Assert.Equal("my-media", data.Name);
        Assert.Equal(parentId, data.ParentId);
        Assert.Equal(new[] { ancestor1, ancestor2 }, data.Ancestors);
    }

    [Fact]
    public async Task MixedPayloads_YieldsEntryForEachEntity()
    {
        Guid deletedId = Guid.NewGuid();
        Guid updatedId = Guid.NewGuid();
        SetupProcessor();

        IPublishedContent media = CreatePublishedMedia(updatedId, "umbracoImage");
        A.CallTo(() => _mediaCache.GetByIdAsync(updatedId)).Returns(media);
        A.CallTo(() => _apiMediaBuilder.Build(media)).Returns(CreateApiMedia(updatedId, "umbracoImage"));
        Guid? outParentId = null;
        A.CallTo(() => _navigationQueryService.TryGetParentKey(updatedId, out outParentId))
            .AssignsOutAndRefParameters((Guid?)null);
        IEnumerable<Guid> outAncestors = null!;
        A.CallTo(() => _navigationQueryService.TryGetAncestorsKeys(updatedId, out outAncestors))
            .Returns(true)
            .AssignsOutAndRefParameters(Array.Empty<Guid>());

        MediaIngestQueueItem item = new(new[]
        {
            new MediaChangePayload(deletedId, MediaChangeType.Delete),
            new MediaChangePayload(updatedId, MediaChangeType.Update)
        });

        List<IngestEntry> entries = await ProcessAsync(item, TestContext.Current.CancellationToken);

        Assert.Equal(3, entries.Count);
        Assert.IsType<DeleteEntry>(entries[0]);
        Assert.IsType<DeleteWhereEntry>(entries[1]);
        UpsertMediaEntry upsertEntry = Assert.IsType<UpsertMediaEntry>(entries[2]);
        Assert.Equal(updatedId.ToString(), upsertEntry.Id);
        Assert.Null(upsertEntry.Data.ParentId);
        Assert.Empty(upsertEntry.Data.Ancestors);
    }

    [Fact]
    public void NullItem_Throws()
    {
        SetupProcessor();

        Assert.Throws<ArgumentNullException>(() => _processor.ProcessAsync(null!, TestContext.Current.CancellationToken));
    }

    private void SetupProcessor()
    {
        _apiMediaBuilder = A.Fake<IApiMediaBuilder>();
        _navigationQueryService = A.Fake<IMediaNavigationQueryService>();
        _mediaCache = A.Fake<IPublishedMediaCache>();
        IUmbracoContext umbracoContext = A.Fake<IUmbracoContext>();
        A.CallTo(() => umbracoContext.Media).Returns(_mediaCache);
        IUmbracoContextAccessor contextAccessor = A.Fake<IUmbracoContextAccessor>();
        IUmbracoContextFactory contextFactory = A.Fake<IUmbracoContextFactory>();
        A.CallTo(() => contextFactory.EnsureUmbracoContext())
            .Returns(new UmbracoContextReference(umbracoContext, false, contextAccessor));

        _processor = new(
            _apiMediaBuilder,
            _navigationQueryService,
            A.Fake<IHttpContextAccessor>(),
            A.Fake<IServiceProvider>(),
            A.Fake<IUmbracoContextAccessor>(),
            contextFactory,
            NullLogger<MediaIngestQueueItemProcessor>.Instance);
    }

    private async Task<List<IngestEntry>> ProcessAsync(MediaIngestQueueItem item, CancellationToken cancellationToken)
    {
        List<IngestEntry> entries = [];
        await foreach (IngestEntry entry in _processor.ProcessAsync(item, cancellationToken))
        {
            entries.Add(entry);
        }

        return entries;
    }

    private IPublishedContent CreatePublishedMedia(in Guid key, string contentTypeAlias)
    {
        IPublishedContent media = _fixture.Create<IPublishedContent>();
        A.CallTo(() => media.Key).Returns(key);
        A.CallTo(() => media.ContentType.Alias).Returns(contentTypeAlias);
        return media;
    }

    private IApiMedia CreateApiMedia(in Guid id, string mediaType)
    {
        IApiMedia apiMedia = _fixture.Create<IApiMedia>();
        A.CallTo(() => apiMedia.Id).Returns(id);
        A.CallTo(() => apiMedia.Name).Returns("my-media");
        A.CallTo(() => apiMedia.MediaType).Returns(mediaType);
        return apiMedia;
    }
}
