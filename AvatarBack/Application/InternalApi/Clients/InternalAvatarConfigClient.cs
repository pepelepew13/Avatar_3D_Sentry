using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AvatarSentry.Application.Config;
using AvatarSentry.Application.InternalApi.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace AvatarSentry.Application.InternalApi.Clients;

public class InternalAvatarConfigClient : IInternalAvatarConfigClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public InternalAvatarConfigClient(HttpClient httpClient, IOptions<InternalApiSettings> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = BuildBaseUri(options.Value.BaseUrl);
    }

    public async Task<PagedResponse<InternalAvatarConfigDto>> GetConfigsAsync(AvatarConfigFilter filter, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["empresa"] = string.IsNullOrWhiteSpace(filter.Empresa) ? null : filter.Empresa,
            ["sede"] = string.IsNullOrWhiteSpace(filter.Sede) ? null : filter.Sede,
            ["page"] = filter.Page.ToString(),
            ["pageSize"] = filter.PageSize.ToString()
        };

        var uri = QueryHelpers.AddQueryString("internal/avatar-config", query);
        var response = await _httpClient.GetAsync(uri, ct);
        response.EnsureSuccessStatusCode();

        var payload = await ReadJsonOrDefaultAsync<PagedResponse<InternalAvatarConfigDto>>(response, ct);
        return payload ?? new PagedResponse<InternalAvatarConfigDto>();
    }

    public async Task<InternalAvatarConfigDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        const int pageSize = 50;
        var page = 1;

        while (true)
        {
            var query = new Dictionary<string, string?>
            {
                ["page"] = page.ToString(),
                ["pageSize"] = pageSize.ToString(),
            };

            var uri = QueryHelpers.AddQueryString("internal/avatar-config", query);

            var response = await _httpClient.GetAsync(uri, ct);
            response.EnsureSuccessStatusCode();

            var payload = await ReadJsonOrDefaultAsync<PagedResponse<InternalAvatarConfigDto>>(response, ct)
                        ?? new PagedResponse<InternalAvatarConfigDto>();

            var items = payload.Items ?? new List<InternalAvatarConfigDto>();
            var match = items.FirstOrDefault(x => x.Id == id);
            if (match is not null)
                return match;

            // cortar si no hay más items
            if (items.Count == 0)
                return null;

            // cortar si ya leímos todo el total
            if (payload.Total > 0 && page * pageSize >= payload.Total)
                return null;

            page++;
        }
    }

    public async Task<InternalAvatarConfigDto?> GetByScopeAsync(string empresa, string sede, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["empresa"] = empresa,
            ["sede"] = sede
        };

        var uri = QueryHelpers.AddQueryString("internal/avatar-config/by-scope", query);
        var response = await _httpClient.GetAsync(uri, ct);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.InternalServerError)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await ReadJsonOrDefaultAsync<InternalAvatarConfigDto>(response, ct);
    }

    public async Task<InternalAvatarConfigDto> CreateAsync(InternalAvatarConfigDto config, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("internal/avatar-config", config, ct);
        response.EnsureSuccessStatusCode();
        await EnsureInternalSuccessAsync(response, ct);

        var fetched = await GetByScopeAsync(config.Empresa, config.Sede, ct);
        return fetched ?? config;
    }

    public async Task<InternalAvatarConfigDto> UpdateAsync(int id, InternalAvatarConfigDto config, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"internal/avatar-config/{id}", config, ct);
        response.EnsureSuccessStatusCode();
        await EnsureInternalSuccessAsync(response, ct);

        var refreshed = await GetByIdAsync(id, ct);
        return refreshed ?? config;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"internal/avatar-config/{id}", ct);
        response.EnsureSuccessStatusCode();
        await EnsureInternalSuccessAsync(response, ct);
    }

    private static Uri BuildBaseUri(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Falta InternalApi:BaseUrl para consumir la API interna.");
        }

        var normalized = NormalizeBaseUrl(baseUrl).TrimEnd('/') + "/";
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"InternalApi:BaseUrl inválido: {baseUrl}");
        }

        return uri;
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        var trimmed = baseUrl.Trim();
        if (trimmed.StartsWith("https:https://", StringComparison.OrdinalIgnoreCase))
        {
            return "https://" + trimmed[14..];
        }

        if (trimmed.StartsWith("http:http://", StringComparison.OrdinalIgnoreCase))
        {
            return "http://" + trimmed[12..];
        }

        return trimmed;
    }

    private async Task<T?> ReadJsonOrDefaultAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.Content is null)
        {
            return default;
        }

        var raw = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(raw, _jsonOptions);
    }

    private static async Task EnsureInternalSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.Content is null)
        {
            return;
        }

        var raw = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            if (!TryGetBoolean(doc.RootElement, "success", out var success) &&
                !TryGetBoolean(doc.RootElement, "Success", out success))
            {
                return;
            }

            if (success)
            {
                return;
            }

            var message = TryGetString(doc.RootElement, "message")
                          ?? TryGetString(doc.RootElement, "Message")
                          ?? "La API interna reportó Success=false.";

            throw new InvalidOperationException(message);
        }
        catch (JsonException)
        {
            // Si no es JSON válido, asumimos que no es el wrapper de Success/Message.
        }
    }

    private static bool TryGetBoolean(JsonElement element, string property, out bool value)
    {
        if (element.TryGetProperty(property, out var prop) &&
            (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False))
        {
            value = prop.GetBoolean();
            return true;
        }

        value = false;
        return false;
    }

    private static string? TryGetString(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }

        return null;
    }
}
