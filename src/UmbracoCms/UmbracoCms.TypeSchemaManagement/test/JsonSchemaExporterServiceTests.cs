using FakeItEasy;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Compose.Integrations.UmbracoCms.Core;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Tests.Unit;

public sealed class JsonSchemaExporterServiceTests
{
    private const string ComposeNodeUrl = "https://umbracocompose.com/v1/node";

    [Fact]
    public async Task GenerateSchemasAsync_ContentType_GeneratesSchemaWithoutXAbstract()
    {
        IContentType contentType = CreateContentType("Test Content", "testContent");
        (JsonSchemaExporterService service, _, IMediaTypeService mediaTypeService) =
            CreateService(WithContentService("testContent", contentType));

        IReadOnlyDictionary<string, JsonSchema> result = await service.GenerateSchemasAsync(new (Constants.UdiEntityType.DocumentType, "testContent"));

        Assert.True(result.ContainsKey("testContent"));
        JsonSchema schema = result["testContent"];
        Assert.Equal("Test Content", schema.Title);
        Assert.Null(schema.Extensions);
        Assert.Contains(schema.AllOf!, x => x.Ref == ComposeNodeUrl);

        A.CallTo(() => mediaTypeService.Get(A<string>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GenerateSchemasAsync_ContentUsedAsComposition_MarksXAbstract()
    {
        IContentType contentType = CreateContentType("Test Content", "testContent");
        (JsonSchemaExporterService service, _, _) =
            CreateService(WithContentService("testContent", contentType, composed: true));

        IReadOnlyDictionary<string, JsonSchema> result = await service.GenerateSchemasAsync(new (Constants.UdiEntityType.DocumentType, "testContent"));

        JsonSchema schema = Assert.Single(result).Value;
        Assert.True(schema.Extensions!["x-abstract"] is true);
        Assert.Null(schema.AllOf);
    }

    [Fact]
    public async Task GenerateSchemasAsync_MediaType_UsesMediaTypeService()
    {
        IMediaType mediaType = CreateMediaType("Test Media", "testMedia");
        (IContentTypeService contentTypeService, IMediaTypeService mediaTypeService) = WithMediaService("testMedia", mediaType);

        (JsonSchemaExporterService service, _, _) = CreateService(contentTypeService, mediaTypeService);

        IReadOnlyDictionary<string, JsonSchema> result = await service.GenerateSchemasAsync(new (Constants.UdiEntityType.MediaType, "testMedia"));

        Assert.True(result.ContainsKey("testMedia"));
        JsonSchema schema = result["testMedia"];
        Assert.Equal("Test Media", schema.Title);
        Assert.Null(schema.Extensions);
        Assert.Contains(schema.AllOf!, x => x.Ref == ComposeNodeUrl);

        A.CallTo(() => mediaTypeService.GetComposedOf(A<int>._)).MustHaveHappenedOnceOrMore();
        A.CallTo(() => contentTypeService.GetComposedOf(A<int>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GenerateSchemasAsync_MediaUsedAsComposition_MarksXAbstract()
    {
        IMediaType mediaType = CreateMediaType("Test Media", "testMedia");
        (IContentTypeService contentTypeService, IMediaTypeService mediaTypeService) = WithMediaService("testMedia", mediaType, composed: true);

        (JsonSchemaExporterService service, _, _) = CreateService(contentTypeService, mediaTypeService);

        IReadOnlyDictionary<string, JsonSchema> result = await service.GenerateSchemasAsync(new (Constants.UdiEntityType.MediaType, "testMedia"));

        JsonSchema schema = Assert.Single(result).Value;
        Assert.True(schema.Extensions!["x-abstract"] is true);
        Assert.Null(schema.AllOf);
    }

    [Fact]
    public async Task GenerateSchemasAsync_AliasNotInEitherService_ReturnsEmptySchemas()
    {
        IContentTypeService contentTypeService = A.Fake<IContentTypeService>();
        A.CallTo(() => contentTypeService.Get("missing")).Returns((IContentType?)null);

        IMediaTypeService mediaTypeService = A.Fake<IMediaTypeService>();
        A.CallTo(() => mediaTypeService.Get("missing")).Returns((IMediaType?)null);

        (JsonSchemaExporterService service, _, _) = CreateService(contentTypeService, mediaTypeService);

        IReadOnlyDictionary<string, JsonSchema> result = await service.GenerateSchemasAsync(new (Constants.UdiEntityType.DocumentType, "missing"));

        Assert.Empty(result);
    }

    private static IContentType CreateContentType(string name, string alias)
    {
        IContentType contentType = A.Fake<IContentType>();
        A.CallTo(() => contentType.Name).Returns(name);
        A.CallTo(() => contentType.Alias).Returns(alias);
        A.CallTo(() => contentType.Id).Returns(1);
        return contentType;
    }

    private static IMediaType CreateMediaType(string name, string alias)
    {
        IMediaType mediaType = A.Fake<IMediaType>();
        A.CallTo(() => mediaType.Name).Returns(name);
        A.CallTo(() => mediaType.Alias).Returns(alias);
        A.CallTo(() => mediaType.Id).Returns(1);
        return mediaType;
    }

    private static IContentTypeService WithContentService(string alias, IContentType contentType, bool composed = false)
    {
        IContentTypeService contentTypeService = A.Fake<IContentTypeService>();
        A.CallTo(() => contentTypeService.Get(alias)).Returns(contentType);
        if (composed)
        {
            A.CallTo(() => contentTypeService.GetComposedOf(contentType.Id)).Returns(new[] { A.Fake<IContentType>() });
        }

        return contentTypeService;
    }

    private static (IContentTypeService ContentTypeService, IMediaTypeService MediaTypeService) WithMediaService(
        string alias,
        IMediaType mediaType,
        bool composed = false)
    {
        IContentTypeService contentTypeService = A.Fake<IContentTypeService>();
        A.CallTo(() => contentTypeService.Get(alias)).Returns((IContentType?)null);

        IMediaTypeService mediaTypeService = A.Fake<IMediaTypeService>();
        A.CallTo(() => mediaTypeService.Get(alias)).Returns(mediaType);
        if (composed)
        {
            A.CallTo(() => mediaTypeService.GetComposedOf(mediaType.Id)).Returns(new[] { A.Fake<IMediaType>() });
        }

        return (contentTypeService, mediaTypeService);
    }

    private static (JsonSchemaExporterService Service, IContentTypeService ContentTypeService, IMediaTypeService MediaTypeService) CreateService(
        IContentTypeService? contentTypeService = null,
        IMediaTypeService? mediaTypeService = null)
    {
        contentTypeService ??= A.Fake<IContentTypeService>();
        mediaTypeService ??= A.Fake<IMediaTypeService>();

        IPublishedContentTypeFactory publishedContentTypeFactory = A.Fake<IPublishedContentTypeFactory>();
        PropertySchemaResolverCollection propertySchemaResolvers = new(() => []);

        IOptionsSnapshot<JsonSchemaGeneratorOptions> jsonSchemaGeneratorOptions = A.Fake<IOptionsSnapshot<JsonSchemaGeneratorOptions>>();
        A.CallTo(() => jsonSchemaGeneratorOptions.Get(A<string>._)).Returns(GeneratorOptions());

        IOptions<UmbracoComposeOptions> options = Options.Create<UmbracoComposeOptions>(new());

        JsonSchemaExporterService service = new(
            contentTypeService,
            mediaTypeService,
            publishedContentTypeFactory,
            propertySchemaResolvers,
            jsonSchemaGeneratorOptions,
            options);

        return (service, contentTypeService, mediaTypeService);
    }

    private static JsonSchemaGeneratorOptions GeneratorOptions() =>
        new()
        {
            DefaultSchema = "https://umbracocompose.com/v1/schema",
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceMode = ReferenceMode.External
        };
}
