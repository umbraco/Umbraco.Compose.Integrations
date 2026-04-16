using PublicApiGenerator;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement.Tests.Unit;

public sealed class PublicApiTests
{
    [Fact]
    public Task AssemblyHasNoPublicApiChanges()
    {
        string publicApi = typeof(SchemaQueueItem).Assembly.GeneratePublicApi();

        return Verify(publicApi);
    }
}
