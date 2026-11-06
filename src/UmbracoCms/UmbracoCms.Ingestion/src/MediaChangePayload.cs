namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Represents a media change payload.
/// </summary>
/// <param name="Id">The media id.</param>
/// <param name="ChangeType">The change type.</param>
public sealed record MediaChangePayload(Guid Id, MediaChangeType ChangeType);
