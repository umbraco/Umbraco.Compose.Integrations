using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using FakeItEasy;
using Umbraco.Cms.Core.Models.DeliveryApi;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Tests.Unit.MediaIngestion;

public sealed class ComposeApiMediaTests
{
    private readonly IFixture _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization { ConfigureMembers = true });

    [Fact]
    public void ExposesParentIdAndAncestors()
    {
        IApiMedia media = _fixture.Create<IApiMedia>();
        Guid parentId = Guid.NewGuid();
        Guid[] ancestors = [Guid.NewGuid(), Guid.NewGuid()];

        ComposeApiMedia apiMedia = new(media, parentId, ancestors);

        Assert.Equal(parentId, apiMedia.ParentId);
        Assert.Equal(ancestors, apiMedia.Ancestors);
    }

    [Fact]
    public void DelegatesPropertiesToWrappedMedia()
    {
        IApiMedia media = _fixture.Create<IApiMedia>();
        Guid id = Guid.NewGuid();
        IDictionary<string, object?> properties = new Dictionary<string, object?> { ["altText"] = "A test image" };
        A.CallTo(() => media.Id).Returns(id);
        A.CallTo(() => media.Name).Returns("my-media");
        A.CallTo(() => media.MediaType).Returns("image/jpeg");
        A.CallTo(() => media.Url).Returns("/media/my-media.jpg");
        A.CallTo(() => media.Extension).Returns("jpg");
        A.CallTo(() => media.Width).Returns(1024);
        A.CallTo(() => media.Height).Returns(768);
        A.CallTo(() => media.Bytes).Returns(20480);
        A.CallTo(() => media.Properties).Returns(properties);

        ComposeApiMedia apiMedia = new(media, null, []);

        Assert.Equal(id, apiMedia.Id);
        Assert.Equal("my-media", apiMedia.Name);
        Assert.Equal("image/jpeg", apiMedia.MediaType);
        Assert.Equal("/media/my-media.jpg", apiMedia.Url);
        Assert.Equal("jpg", apiMedia.Extension);
        Assert.Equal(1024, apiMedia.Width);
        Assert.Equal(768, apiMedia.Height);
        Assert.Equal(20480, apiMedia.Bytes);
        Assert.Same(properties, apiMedia.Properties);
    }
}
