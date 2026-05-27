namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Represents a delete where action.
/// </summary>
public sealed class DeleteWhereEntry : IngestEntry
{
    /// <inheritdoc />
    public override string Action { get; } = "delete";

    /// <summary>
    /// A dictionary of properties and values the where should match.
    /// </summary>
    public required Dictionary<string, object?> Where { get; init; }
}
