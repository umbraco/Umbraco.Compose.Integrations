using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Compose.Integrations.UmbracoCms.Core;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Tests.Unit.MediaIngestion;

public sealed class MediaNotificationHandlerTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization { ConfigureMembers = true });

    [Fact]
    public async Task SavedNotification_EnqueuesUpdatePayloads()
    {
        IMedia media1 = _fixture.Create<IMedia>();
        IMedia media2 = _fixture.Create<IMedia>();

        (MediaNotificationHandler handler, List<IngestQueueItem> enqueued) = CreateHandler();

        await handler.HandleAsync(
            new MediaSavedNotification(new[] { media1, media2 }, new EventMessages()),
            CancellationToken.None);

        MediaIngestQueueItem item = Assert.IsType<MediaIngestQueueItem>(Assert.Single(enqueued));
        Assert.Equal(
            new[] { new MediaChangePayload(media1.Key, MediaChangeType.Update), new MediaChangePayload(media2.Key, MediaChangeType.Update) },
            item.Entities);
    }

    [Fact]
    public async Task DeletedNotification_EnqueuesDeletePayloads()
    {
        IMedia media = _fixture.Create<IMedia>();

        (MediaNotificationHandler handler, List<IngestQueueItem> enqueued) = CreateHandler();

        await handler.HandleAsync(
            new MediaDeletedNotification(media, new EventMessages()),
            CancellationToken.None);

        MediaIngestQueueItem item = Assert.IsType<MediaIngestQueueItem>(Assert.Single(enqueued));
        Assert.Equal(new[] { new MediaChangePayload(media.Key, MediaChangeType.Delete) }, item.Entities);
    }

    [Fact]
    public async Task MovedNotification_EnqueuesUpdatePayloads()
    {
        IMedia media1 = _fixture.Create<IMedia>();
        IMedia media2 = _fixture.Create<IMedia>();
        Guid parentKey = Guid.NewGuid();

        (MediaNotificationHandler handler, List<IngestQueueItem> enqueued) = CreateHandler();

        await handler.HandleAsync(
            new MediaMovedNotification(
                new[] { new MoveEventInfo<IMedia>(media1, "parent/child1", parentKey), new MoveEventInfo<IMedia>(media2, "parent/child2", parentKey) },
                new EventMessages()),
            CancellationToken.None);

        MediaIngestQueueItem item = Assert.IsType<MediaIngestQueueItem>(Assert.Single(enqueued));
        Assert.Equal(
            new[] { new MediaChangePayload(media1.Key, MediaChangeType.Update), new MediaChangePayload(media2.Key, MediaChangeType.Update) },
            item.Entities);
    }

    [Fact]
    public async Task MovedToRecycleBinNotification_EnqueuesDeletePayloads()
    {
        IMedia media = _fixture.Create<IMedia>();

        (MediaNotificationHandler handler, List<IngestQueueItem> enqueued) = CreateHandler();

        await handler.HandleAsync(
            new MediaMovedToRecycleBinNotification(new[] { new MoveToRecycleBinEventInfo<IMedia>(media, "parent/child") }, new EventMessages()),
            CancellationToken.None);

        MediaIngestQueueItem item = Assert.IsType<MediaIngestQueueItem>(Assert.Single(enqueued));
        Assert.Equal(new[] { new MediaChangePayload(media.Key, MediaChangeType.Delete) }, item.Entities);
    }

    [Fact]
    public async Task ComposeOptionsNotValid_DoesNotEnqueue()
    {
        IMedia media = _fixture.Create<IMedia>();

        (MediaNotificationHandler handler, List<IngestQueueItem> enqueued) = CreateHandler(
            composeOptions: new UmbracoComposeOptions());

        await handler.HandleAsync(
            new MediaSavedNotification(new[] { media }, new EventMessages()),
            CancellationToken.None);

        Assert.Empty(enqueued);
    }

    [Fact]
    public async Task IngestionOptionsNotValid_DoesNotEnqueue()
    {
        IMedia media = _fixture.Create<IMedia>();

        (MediaNotificationHandler handler, List<IngestQueueItem> enqueued) = CreateHandler(
            ingestionOptions: new UmbracoComposeIngestionOptions());

        await handler.HandleAsync(
            new MediaSavedNotification(new[] { media }, new EventMessages()),
            CancellationToken.None);

        Assert.Empty(enqueued);
    }

    [Fact]
    public async Task IngestionOptionsWithCollectionAlias_Enqueues()
    {
        IMedia media = _fixture.Create<IMedia>();

        (MediaNotificationHandler handler, List<IngestQueueItem> enqueued) = CreateHandler(
            ingestionOptions: new UmbracoComposeIngestionOptions { CollectionAlias = "content" });

        await handler.HandleAsync(
            new MediaSavedNotification(new[] { media }, new EventMessages()),
            CancellationToken.None);

        Assert.IsType<MediaIngestQueueItem>(Assert.Single(enqueued));
    }

    [Fact]
    public async Task IngestionOptionsWithCollections_Enqueues()
    {
        IMedia media = _fixture.Create<IMedia>();

        (MediaNotificationHandler handler, List<IngestQueueItem> enqueued) = CreateHandler(
            ingestionOptions: new UmbracoComposeIngestionOptions
            {
                Collections = new()
                {
                    Content = "content",
                    Media = "media"
                }
            });

        await handler.HandleAsync(
            new MediaSavedNotification(new[] { media }, new EventMessages()),
            CancellationToken.None);

        Assert.IsType<MediaIngestQueueItem>(Assert.Single(enqueued));
    }

    private static UmbracoComposeOptions ValidComposeOptions() =>
        new()
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            EnvironmentAlias = "environment-alias",
            ProjectAlias = "project-alias",
            Region = "eu"
        };

    private (MediaNotificationHandler Handler, List<IngestQueueItem> Enqueued) CreateHandler(
        UmbracoComposeOptions? composeOptions = null,
        UmbracoComposeIngestionOptions? ingestionOptions = null)
    {
        List<IngestQueueItem> enqueued = [];
        IIngestService ingestService = A.Fake<IIngestService>();
        A.CallTo(() => ingestService.EnqueueAsync(A<IngestQueueItem>._, A<CancellationToken>._))
            .Invokes((IngestQueueItem item, CancellationToken _) => enqueued.Add(item));
        ICoreScopeProvider coreScopeProvider = A.Fake<ICoreScopeProvider>();
        A.CallTo(() => coreScopeProvider.Context).Returns((IScopeContext?)null);

        MediaNotificationHandler handler = new(
            coreScopeProvider,
            ingestService,
            Options.Create(composeOptions ?? ValidComposeOptions()),
            Options.Create(ingestionOptions ?? new() { CollectionAlias = "content" }),
            NullLogger<MediaNotificationHandler>.Instance);

        return (handler, enqueued);
    }
}
