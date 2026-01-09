using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services.Changes;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Cache.Content;

internal sealed class PublishedContentCacheRefresher(
    AppCaches appCaches,
    IJsonSerializer serializer,
    IEventAggregator eventAggregator,
    ICacheRefresherNotificationFactory factory)
    : PayloadCacheRefresherBase<PublishedContentCacheRefresherNotification, PublishedContentCacheRefresher.JsonPayload>(
        appCaches,
        serializer,
        eventAggregator,
        factory)
{
    public static readonly Guid UniqueId = Guid.Parse("3c8ac118-9259-46f9-8a6b-2405ea0bcfff");

    public override Guid RefresherUniqueId =>
        UniqueId;

    public override string Name =>
        "Published Content Cache Refresher";

    public sealed record JsonPayload(Guid ContentKey, TreeChangeTypes ChangeTypes, string[] AffectedCultures);
}
