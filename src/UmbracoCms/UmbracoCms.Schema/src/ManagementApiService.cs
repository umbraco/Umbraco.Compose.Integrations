using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Compose.Integrations.UmbracoCms.Schema;

internal class ManagementApiService(HttpClient httpClient)
{
    internal async Task<TypeSchemaDto?> GetTypeSchemaAsync(
        string typeSchemaAlias,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            $"type-schemas/{typeSchemaAlias}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TypeSchemaDto>(cancellationToken);
    }

    internal async Task<TypeSchemaDto?> CreateTypeSchemaAsync(
        CreateTypeSchemaRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "type-schemas",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Request failed with status {response.StatusCode}: {errorContent}");
        }

        return await response.Content.ReadFromJsonAsync<TypeSchemaDto>(cancellationToken);
    }

    internal async Task<TypeSchemaDto?> UpdateTypeSchemaSchemaAsync(
        string typeSchemaAlias,
        JsonElement schema,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync(
            $"type-schemas/{typeSchemaAlias}/commands/update-schema",
            schema,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TypeSchemaDto>(cancellationToken);
    }
}

internal record TypeSchemaDto(
    [property: JsonPropertyName("typeSchemaAlias")]
    string TypeSchemaAlias,
    [property: JsonPropertyName("description")]
    string? Description,
    [property: JsonPropertyName("schema")] JsonElement Schema
);