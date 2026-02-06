using PublicApiGenerator;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Tests.Unit;

public sealed class PublicApiTests
{
    [Fact]
    public Task AssemblyHasNoPublicApiChanges()
    {
        string publicApi = typeof(IIngestService).Assembly.GeneratePublicApi();

        return Verify(publicApi);
    }
}
