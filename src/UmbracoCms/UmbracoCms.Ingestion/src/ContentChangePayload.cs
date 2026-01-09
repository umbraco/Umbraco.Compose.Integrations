using Umbraco.Cms.Core.Services.Changes;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion;

public sealed record ContentChangePayload(Guid Id, TreeChangeTypes ChangeTypes, IReadOnlyCollection<string> AffectedCultures);
