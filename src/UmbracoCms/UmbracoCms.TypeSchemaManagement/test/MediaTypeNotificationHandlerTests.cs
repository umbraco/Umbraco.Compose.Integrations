using System.Threading.Channels;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Persistence;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Tests.Unit;

public sealed class MediaTypeNotificationHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidOptions_InsertsQueueItemAndWritesToChannel()
    {
        IMediaType mediaType = CreateMediaType("testMedia");

        (MediaTypeNotificationHandler handler, List<SchemaQueueDto> inserted, List<SchemaQueueItem> written) = CreateHandler();

        await handler.HandleAsync(
            new MediaTypeSavedNotification([mediaType], new EventMessages()),
            CancellationToken.None);

        SchemaQueueDto insertedDto = Assert.Single(inserted);
        Assert.Equal("umb://media-type/testMedia", insertedDto.ContentTypeAlias);
        Assert.NotEqual(Guid.Empty, insertedDto.Id);
        Assert.True(insertedDto.CreatedAt <= DateTime.UtcNow);

        SchemaQueueItem item = Assert.Single(written);
        Assert.Equal(insertedDto.Id, item.Id);
        Assert.Equal(insertedDto.ContentTypeAlias, item.ContentTypeAlias.ToString());
    }

    [Fact]
    public async Task HandleAsync_MultipleSavedTypes_InsertsAndWritesForEach()
    {
        IMediaType mediaType1 = CreateMediaType("mediaOne");
        IMediaType mediaType2 = CreateMediaType("mediaTwo");

        (MediaTypeNotificationHandler handler, List<SchemaQueueDto> inserted, List<SchemaQueueItem> written) = CreateHandler();

        await handler.HandleAsync(
            new MediaTypeSavedNotification([mediaType1, mediaType2], new EventMessages()),
            CancellationToken.None);

        Assert.Equal(["umb://media-type/mediaOne", "umb://media-type/mediaTwo"], inserted.Select(x => x.ContentTypeAlias));
        Assert.Equal(["umb://media-type/mediaOne", "umb://media-type/mediaTwo"], written.Select(x => x.ContentTypeAlias.ToString()));

        for (int i = 0; i < inserted.Count; i++)
        {
            Assert.Equal(inserted[i].Id, written[i].Id);
        }
    }

    [Fact]
    public async Task HandleAsync_OptionsNotValid_DoesNotInsertOrWrite()
    {
        IMediaType mediaType = CreateMediaType("testMedia");

        (MediaTypeNotificationHandler handler, List<SchemaQueueDto> inserted, List<SchemaQueueItem> written) = CreateHandler(
            options: new UmbracoComposeOptions());

        await handler.HandleAsync(
            new MediaTypeSavedNotification([mediaType], new EventMessages()),
            CancellationToken.None);

        Assert.Empty(inserted);
        Assert.Empty(written);
    }

    private static IMediaType CreateMediaType(string alias)
    {
        IMediaType mediaType = A.Fake<IMediaType>();
        A.CallTo(() => mediaType.Alias).Returns(alias);
        return mediaType;
    }

    private static (MediaTypeNotificationHandler Handler, List<SchemaQueueDto> Inserted, List<SchemaQueueItem> Written) CreateHandler(
        UmbracoComposeOptions? options = null)
    {
        List<SchemaQueueDto> inserted = [];
        List<SchemaQueueItem> written = [];

        ISchemaQueueRepository queueRepository = A.Fake<ISchemaQueueRepository>();
        A.CallTo(() => queueRepository.InsertAsync(A<SchemaQueueDto>._, A<CancellationToken>._))
            .Invokes((SchemaQueueDto dto, CancellationToken _) => inserted.Add(dto));

        ChannelWriter<SchemaQueueItem> writer = A.Fake<ChannelWriter<SchemaQueueItem>>();
        A.CallTo(() => writer.WriteAsync(A<SchemaQueueItem>._, A<CancellationToken>._))
            .Invokes((SchemaQueueItem item, CancellationToken _) => written.Add(item));

        ICoreScopeProvider coreScopeProvider = A.Fake<ICoreScopeProvider>();
        A.CallTo(() => coreScopeProvider.Context).Returns((IScopeContext?)null);

        MediaTypeNotificationHandler handler = new(
            writer,
            queueRepository,
            coreScopeProvider,
            NullLogger<MediaTypeNotificationHandler>.Instance,
            Options.Create(options ?? ValidOptions()));

        return (handler, inserted, written);
    }

    private static UmbracoComposeOptions ValidOptions() =>
        new()
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            EnvironmentAlias = "environment-alias",
            ProjectAlias = "project-alias",
            Region = "eu"
        };
}
