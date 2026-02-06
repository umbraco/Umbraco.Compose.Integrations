using PublicApiGenerator;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Tests.Unit;

public sealed class PublicApiTests
{
    [Fact]
    public Task AssemblyHasNoPublicApiChanges()
    {
        string publicApi = typeof(ComposeNode).Assembly.GeneratePublicApi();

        return Verify(publicApi);
    }
}
