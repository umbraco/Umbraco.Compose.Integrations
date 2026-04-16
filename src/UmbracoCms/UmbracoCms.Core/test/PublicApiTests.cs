using PublicApiGenerator;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Tests.Unit;

public sealed class PublicApiTests
{
    [Fact]
    public Task AssemblyHasNoPublicApiChanges()
    {
        string publicApi = typeof(JsonSchema).Assembly.GeneratePublicApi();

        return Verify(publicApi);
    }
}
