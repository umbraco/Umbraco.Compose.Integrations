using PublicApiGenerator;

namespace Umbraco.Compose.Integrations.UmbracoCms.DataSource.Tests.Unit;

public sealed class PublicApiTests
{
    [Fact]
    public Task AssemblyHasNoPublicApiChanges()
    {
        string publicApi = typeof(UmbracoComposeDataSourceController).Assembly.GeneratePublicApi();

        return Verify(publicApi);
    }
}
