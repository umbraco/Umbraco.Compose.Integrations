namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Represents a delete action.
/// </summary>
public sealed class DeleteEntry : IngestEntry
{
    /// <inheritdoc />
    public override string Action { get; } = "delete";
}
