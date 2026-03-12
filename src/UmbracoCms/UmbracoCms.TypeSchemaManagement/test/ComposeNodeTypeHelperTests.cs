using Umbraco.Cms.Core.Models.DeliveryApi;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Tests.Unit;

public class ComposeNodeTypeHelperTests
{
    [Theory]
    [InlineData(typeof(IApiContent), true)]
    [InlineData(typeof(IApiMedia), true)]
    [InlineData(typeof(IApiContentResponse), true)]
    [InlineData(typeof(IApiMediaWithCrops), true)]
    [InlineData(typeof(IApiElement), false)]
    [InlineData(typeof(IApiContentRoute), false)]
    [InlineData(typeof(string), false)]
    public void IsComposeNodeType_ReturnsExpectedResult(Type type, bool expected)
    {
        bool result = ComposeNodeTypeHelper.IsComposeNodeType(type);

        Assert.Equal(expected, result);
    }
}
