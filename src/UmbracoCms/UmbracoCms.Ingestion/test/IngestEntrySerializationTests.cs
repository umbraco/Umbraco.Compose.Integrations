using System.Text.Json;
using System.Text.Json.Serialization;
using Umbraco.Cms.Core.Models.DeliveryApi;

namespace Umbraco.Compose.Integrations.UmbracoCms.Ingestion.Tests.Unit;

public sealed class IngestEntrySerializationTests
{
    // Must match the serializer options in IngestBackgroundService -
    // these tests pin the exact wire format sent to the Compose ingestion API.
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    private static readonly Guid s_mediaId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid s_parentId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid s_ancestorId = new("33333333-3333-3333-3333-333333333333");

    [Fact]
    public Task UpsertMediaEntry_SerializesWithActionIdTypeAndData()
    {
        List<IngestEntry> entries =
        [
            new UpsertMediaEntry
            {
                Id = s_mediaId.ToString(),
                Type = "umbracoImage",
                Data = new(new FixedApiMedia(s_mediaId), s_parentId, [s_ancestorId])
            }
        ];

        string json = JsonSerializer.Serialize(entries, s_jsonSerializerOptions);

        return Verify(json);
    }

    [Fact]
    public Task DeleteEntries_SerializeWithWhereClause()
    {
        List<IngestEntry> entries =
        [
            new DeleteEntry { Id = s_mediaId.ToString() },
            new DeleteWhereEntry
            {
                Where = new()
                {
                    ["ancestors_some"] = new[] { s_mediaId.ToString() }
                }
            }
        ];

        string json = JsonSerializer.Serialize(entries, s_jsonSerializerOptions);

        return Verify(json);
    }

    private sealed class FixedApiMedia(Guid id) : IApiMedia
    {
        public Guid Id { get; } = id;

        public string Name { get; } = "my-media";

        public string MediaType { get; } = "image/jpeg";

        public string Url { get; } = "/media/my-media.jpg";

        public string? Extension { get; } = "jpg";

        public int? Width { get; } = 1024;

        public int? Height { get; } = 768;

        public int? Bytes { get; } = 20480;

        public IDictionary<string, object?> Properties { get; } =
            new Dictionary<string, object?> { ["altText"] = "A test image" };
    }
}
