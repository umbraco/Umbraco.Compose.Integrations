namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

public sealed class AddContentEntry : UpsertEntry<ComposeApiContent>
{
    public required string Type { get; set; }
}
