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

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<InternalAvatarConfigDto>>(_jsonOptions, ct);
        return payload ?? new PagedResponse<InternalAvatarConfigDto>();
    }

    public async Task<InternalAvatarConfigDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"internal/avatar-config/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InternalAvatarConfigDto>(_jsonOptions, ct);
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
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InternalAvatarConfigDto>(_jsonOptions, ct);
    }

    public async Task<InternalAvatarConfigDto> CreateAsync(InternalAvatarConfigDto config, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("internal/avatar-config", config, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<InternalAvatarConfigDto>(_jsonOptions, ct);
        return payload ?? config;
    }

    public async Task<InternalAvatarConfigDto> UpdateAsync(int id, InternalAvatarConfigDto config, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"internal/avatar-config/{id}", config, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<InternalAvatarConfigDto>(_jsonOptions, ct);
        return payload ?? config;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"internal/avatar-config/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    private static Uri BuildBaseUri(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Falta InternalApi:BaseUrl para consumir la API interna.");
        }

        var normalized = baseUrl.Trim().TrimEnd('/') + "/";
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"InternalApi:BaseUrl inválido: {baseUrl}");
        }

        return uri;
    }
}
