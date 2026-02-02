using Umbraco.Cms.Core.Services.Changes;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

/// <summary>
/// Represents a content change payload.
/// </summary>
/// <param name="Id">The content id.</param>
/// <param name="ChangeTypes">The change types.</param>
/// <param name="AffectedCultures">The affected cultures.</param>
public sealed record ContentChangePayload(Guid Id, TreeChangeTypes ChangeTypes, IReadOnlyCollection<string> AffectedCultures);
