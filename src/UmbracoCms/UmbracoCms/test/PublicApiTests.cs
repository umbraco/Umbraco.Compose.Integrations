using PublicApiGenerator;

namespace Umbraco.Compose.Integrations.UmbracoCms.Tests.Unit;

public sealed class PublicApiTests
{
    [Fact]
    public Task AssemblyHasNoPublicApiChanges()
    {
        string publicApi = typeof(UmbracoComposeComposer).Assembly.GeneratePublicApi();

        return Verify(publicApi);
    }
}
