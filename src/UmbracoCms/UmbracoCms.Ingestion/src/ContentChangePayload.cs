namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Represents a content change payload.
/// </summary>
/// <param name="Id">The content id.</param>
/// <param name="ChangeType">The change types.</param>
/// <param name="AffectedCultures">The affected cultures.</param>
public sealed record ContentChangePayload(Guid Id, ContentChangeType ChangeType, IReadOnlyCollection<string> AffectedCultures);
