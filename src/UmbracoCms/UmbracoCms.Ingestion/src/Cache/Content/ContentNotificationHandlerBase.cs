using Umbraco.Cms.Core.Models;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Cache.Content;

internal abstract class ContentNotificationHandlerBase
{
    protected static T[] FindTopmostEntities<T>(IEnumerable<T> candidates)
        where T : IContentBase
    {
        T[] candidatesAsArray = candidates as T[] ?? [.. candidates];
        int[] ids = [.. candidatesAsArray.Select(entity => entity.Id)];
        return [.. candidatesAsArray.Where(entity => !ids.Contains(entity.ParentId))];
    }
}
