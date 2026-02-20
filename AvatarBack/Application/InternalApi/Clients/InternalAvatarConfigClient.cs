using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AvatarSentry.Application.Config;
using AvatarSentry.Application.Exceptions;
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
            ["company"] = filter.Company.HasValue ? filter.Company.Value.ToString() : null,
            ["site"] = filter.Site.HasValue ? filter.Site.Value.ToString() : null,
            ["page"] = Math.Max(filter.Page, 1).ToString(),
            ["pageSize"] = Math.Max(filter.PageSize, 1).ToString()
        };

        var uri = QueryHelpers.AddQueryString("internal/avatar-config", query);
        var response = await _httpClient.GetAsync(uri, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await ReadBodyAsync(response, ct);
            throw CreateInternalApiException(response.StatusCode, body, "Error consultando avatar-config en API interna.");
        }

        var payload = await ReadJsonOrDefaultAsync<InternalAvatarConfigPagedResponse>(response, ct);
        if (payload is null)
        {
            throw new AvatarSentryException("La API interna devolvió una respuesta vacía para avatar-config.", 502);
        }

        return new PagedResponse<InternalAvatarConfigDto>
        {
            Page = payload.Page,
            PageSize = payload.PageSize,
            Total = payload.Total,
            TotalPages = payload.TotalPages,
            Items = payload.Items ?? new List<InternalAvatarConfigDto>()
        };
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

            var payload = await ReadJsonOrDefaultAsync<InternalAvatarConfigPagedResponse>(response, ct);
            var items = payload?.Items ?? new List<InternalAvatarConfigDto>();
            var match = items.FirstOrDefault(x => x.Id == id);
            if (match is not null)
                return match;

            if (items.Count == 0)
                return null;
            if (payload is not null && payload.Total > 0 && page * pageSize >= payload.Total)
                return null;

            page++;
        }
    }

    public async Task<InternalAvatarConfigDto?> GetByScopeAsync(int? company, int? site, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>();
        if (company.HasValue) query["company"] = company.Value.ToString();
        if (site.HasValue) query["site"] = site.Value.ToString();

        if (query.Count == 0)
            return null;

        var uri = QueryHelpers.AddQueryString("internal/avatar-config/by-scope", query);
        var response = await _httpClient.GetAsync(uri, ct);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.InternalServerError)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await ReadJsonOrDefaultAsync<InternalAvatarConfigDto>(response, ct);
    }

    public async Task<InternalAvatarConfigDto> CreateAsync(CreateInternalAvatarConfigRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("internal/avatar-config", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await ReadBodyAsync(response, ct);
            throw CreateInternalApiException(response.StatusCode, body, "Error creando avatar-config en API interna.");
        }
        await EnsureInternalSuccessAsync(response, ct);

        var fetched = await GetByScopeAsync(request.CompanyId, request.SiteId, ct);
        if (fetched is not null)
            return fetched;
        return new InternalAvatarConfigDto
        {
            CompanyId = request.CompanyId,
            SiteId = request.SiteId,
            ModelUrl = request.ModelPath,
            BackgroundUrl = request.BackgroundPath,
            LogoUrl = request.LogoPath,
            Language = request.Language,
            HairColor = request.HairColor,
            VoiceIds = request.VoiceIds ?? Array.Empty<int>(),
            Status = request.Status,
            IsActive = request.IsActive
        };
    }

    public async Task<InternalAvatarConfigDto> UpdateAsync(int id, UpdateInternalAvatarConfigRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"internal/avatar-config/{id}", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await ReadBodyAsync(response, ct);
            throw CreateInternalApiException(response.StatusCode, body, "Error actualizando avatar-config en API interna.");
        }
        await EnsureInternalSuccessAsync(response, ct);

        var refreshed = await GetByIdAsync(id, ct);
        if (refreshed is not null)
            return refreshed;
        return new InternalAvatarConfigDto
        {
            Id = id,
            CompanyId = request.CompanyId,
            SiteId = request.SiteId,
            ModelUrl = request.ModelPath,
            BackgroundUrl = request.BackgroundPath,
            LogoUrl = request.LogoPath,
            Language = request.Language,
            HairColor = request.HairColor,
            VoiceIds = request.VoiceIds ?? Array.Empty<int>(),
            Status = request.Status,
            IsActive = request.IsActive
        };
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"internal/avatar-config/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await ReadBodyAsync(response, ct);
            throw CreateInternalApiException(response.StatusCode, body, "Error eliminando avatar-config en API interna.");
        }
        await EnsureInternalSuccessAsync(response, ct);
    }

    internal static Uri BuildBaseUri(string baseUrl)
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

    private static async Task<string?> ReadBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.Content is null)
        {
            return null;
        }

        var raw = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrWhiteSpace(raw) ? null : raw;
    }

    private static AvatarSentryException CreateInternalApiException(HttpStatusCode statusCode, string? body, string message)
    {
        var status = statusCode switch
        {
            HttpStatusCode.BadRequest => 502,
            HttpStatusCode.NotFound => 404,
            HttpStatusCode.InternalServerError => 502,
            HttpStatusCode.Unauthorized => 502,
            HttpStatusCode.Forbidden => 502,
            _ => 502
        };

        var detail = string.IsNullOrWhiteSpace(body) ? null : body;
        return new AvatarSentryException(message, status, detail);
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
