using System.Threading.Channels;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Persistence;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Tests.Unit;

public sealed class ContentTypeNotificationHandlerTests
{
    [Fact]
    public async Task HandleContentTypeAsync_ValidOptions_InsertsQueueItemAndWritesToChannel()
    {
        IContentType contentType = CreateContentType("testContent");

        (ContentTypeNotificationHandler handler, List<SchemaQueueDto> inserted, List<SchemaQueueItem> written) = CreateHandler();

        await handler.HandleAsync(
            new ContentTypeSavedNotification([contentType], new EventMessages()),
            CancellationToken.None);

        SchemaQueueDto insertedDto = Assert.Single(inserted);
        Assert.Equal("umb://document-type/testContent", insertedDto.ContentTypeAlias);
        Assert.NotEqual(Guid.Empty, insertedDto.Id);

        SchemaQueueItem item = Assert.Single(written);
        Assert.Equal(insertedDto.Id, item.Id);
        Assert.Equal(insertedDto.ContentTypeAlias, item.ContentTypeAlias.ToString());
    }

    [Fact]
    public async Task HandleDataTypeAsync_ValidOptions_InsertsAndWritesForEachRelatedContentType()
    {
        IDataType dataType = CreateDataType();

        (ContentTypeNotificationHandler handler, List<SchemaQueueDto> inserted, List<SchemaQueueItem> written) = CreateHandler(
            dataTypeService: CreateDataTypeService(["contentOne", "contentTwo"]));

        await handler.HandleAsync(
            new DataTypeSavedNotification([dataType], new EventMessages()),
            CancellationToken.None);

        Assert.Equal(["umb://document-type/contentOne", "umb://document-type/contentTwo"], inserted.Select(x => x.ContentTypeAlias));
        Assert.Equal(["umb://document-type/contentOne", "umb://document-type/contentTwo"], written.Select(x => x.ContentTypeAlias.ToString()));

        for (int i = 0; i < inserted.Count; i++)
        {
            Assert.Equal(inserted[i].Id, written[i].Id);
        }
    }

    [Fact]
    public async Task HandleDataTypeAsync_NoRelations_DoesNotInsertOrWrite()
    {
        IDataType dataType = CreateDataType();

        (ContentTypeNotificationHandler handler, List<SchemaQueueDto> inserted, List<SchemaQueueItem> written) = CreateHandler(
            dataTypeService: CreateDataTypeService([]));

        await handler.HandleAsync(
            new DataTypeSavedNotification([dataType], new EventMessages()),
            CancellationToken.None);

        Assert.Empty(inserted);
        Assert.Empty(written);
    }

    [Fact]
    public async Task HandleContentTypeAsync_OptionsNotValid_DoesNotInsertOrWrite()
    {
        IContentType contentType = CreateContentType("testContent");

        (ContentTypeNotificationHandler handler, List<SchemaQueueDto> inserted, List<SchemaQueueItem> written) = CreateHandler(
            options: new UmbracoComposeOptions());

        await handler.HandleAsync(
            new ContentTypeSavedNotification([contentType], new EventMessages()),
            CancellationToken.None);

        Assert.Empty(inserted);
        Assert.Empty(written);
    }

    [Fact]
    public async Task HandleDataTypeAsync_OptionsNotValid_DoesNotInsertOrWrite()
    {
        IDataType dataType = CreateDataType();

        (ContentTypeNotificationHandler handler, List<SchemaQueueDto> inserted, List<SchemaQueueItem> written) = CreateHandler(
            options: new UmbracoComposeOptions());

        await handler.HandleAsync(
            new DataTypeSavedNotification([dataType], new EventMessages()),
            CancellationToken.None);

        Assert.Empty(inserted);
        Assert.Empty(written);
    }

    private static IContentType CreateContentType(string alias)
    {
        IContentType contentType = A.Fake<IContentType>();
        A.CallTo(() => contentType.Alias).Returns(alias);
        return contentType;
    }

    private static IDataType CreateDataType()
    {
        IDataType dataType = A.Fake<IDataType>();
        A.CallTo(() => dataType.Key).Returns(Guid.NewGuid());
        return dataType;
    }

    private static IDataTypeService CreateDataTypeService(string[] contentTypeAliases)
    {
        IDataTypeService dataTypeService = A.Fake<IDataTypeService>();
        A.CallTo(() => dataTypeService.GetPagedRelationsAsync(A<Guid>._, A<int>._, A<int>._))
            .Returns(Task.FromResult(new PagedModel<RelationItemModel>(
                contentTypeAliases.Length,
                contentTypeAliases.Select(alias => new RelationItemModel { ContentTypeAlias = alias }))));
        return dataTypeService;
    }

    private static (ContentTypeNotificationHandler Handler, List<SchemaQueueDto> Inserted, List<SchemaQueueItem> Written) CreateHandler(
        IDataTypeService? dataTypeService = null,
        UmbracoComposeOptions? options = null)
    {
        List<SchemaQueueDto> inserted = [];
        List<SchemaQueueItem> written = [];

        IEntityService entityService = A.Fake<IEntityService>();
        A.CallTo(() => entityService.GetObjectType(A<Guid>._))
            .Returns(UmbracoObjectTypes.DocumentType);

        ISchemaQueueRepository queueRepository = A.Fake<ISchemaQueueRepository>();
        A.CallTo(() => queueRepository.InsertAsync(A<SchemaQueueDto>._, A<CancellationToken>._))
            .Invokes((SchemaQueueDto dto, CancellationToken _) => inserted.Add(dto));

        ChannelWriter<SchemaQueueItem> writer = A.Fake<ChannelWriter<SchemaQueueItem>>();
        A.CallTo(() => writer.WriteAsync(A<SchemaQueueItem>._, A<CancellationToken>._))
            .Invokes((SchemaQueueItem item, CancellationToken _) => written.Add(item));

        ICoreScopeProvider coreScopeProvider = A.Fake<ICoreScopeProvider>();
        A.CallTo(() => coreScopeProvider.Context).Returns((IScopeContext?)null);

        ContentTypeNotificationHandler handler = new(
            writer,
            entityService,
            queueRepository,
            dataTypeService ?? A.Fake<IDataTypeService>(),
            coreScopeProvider,
            NullLogger<ContentTypeNotificationHandler>.Instance,
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
