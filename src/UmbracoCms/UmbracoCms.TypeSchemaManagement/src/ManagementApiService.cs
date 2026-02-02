using System.Net.Http.Json;
using System.Text.Json;

namespace Umbraco.Compose.Integrations.UmbracoCms.TypeSchemaManagement;

internal class ManagementApiService(HttpClient httpClient)
{
    internal async Task<TypeSchemaDto?> GetTypeSchemaAsync(
        string typeSchemaAlias,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.GetAsync(
            $"type-schemas/{typeSchemaAlias}",
            cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        _ = response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TypeSchemaDto>(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<TypeSchemaDto?> CreateTypeSchemaAsync(
        CreateTypeSchemaRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "type-schemas",
            request,
            cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"Request failed with status {response.StatusCode}: {errorContent}");
        }

        return await response.Content.ReadFromJsonAsync<TypeSchemaDto>(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<TypeSchemaDto?> UpdateTypeSchemaSchemaAsync(
        string typeSchemaAlias,
        JsonElement schema,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.PutAsJsonAsync(
            $"type-schemas/{typeSchemaAlias}/commands/update-schema",
            schema,
            cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TypeSchemaDto>(cancellationToken).ConfigureAwait(false);
    }
}
