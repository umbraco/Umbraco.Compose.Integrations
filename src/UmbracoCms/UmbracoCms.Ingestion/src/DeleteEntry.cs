namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

public sealed class DeleteEntry : IngestEntry
{
    public override string Action { get; } = "delete";
}
