using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AvatarSentry.Application.Config;
using AvatarSentry.Application.InternalApi.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace AvatarSentry.Application.InternalApi.Clients;

public class InternalUserClient : IInternalUserClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public InternalUserClient(HttpClient httpClient, IOptions<InternalApiSettings> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = BuildBaseUri(options.Value.BaseUrl);
    }

    public async Task<PagedResponse<InternalUserDto>> GetUsersAsync(UserFilter filter, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["empresa"] = string.IsNullOrWhiteSpace(filter.Empresa) ? null : filter.Empresa,
            ["sede"] = string.IsNullOrWhiteSpace(filter.Sede) ? null : filter.Sede,
            ["role"] = string.IsNullOrWhiteSpace(filter.Role) ? null : filter.Role,
            ["q"] = string.IsNullOrWhiteSpace(filter.Q) ? null : filter.Q,
            ["page"] = filter.Page.ToString(),
            ["pageSize"] = filter.PageSize.ToString()
        };

        var uri = QueryHelpers.AddQueryString("internal/users", query);
        var response = await _httpClient.GetAsync(uri, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<InternalUserDto>>(_jsonOptions, ct);
        return payload ?? new PagedResponse<InternalUserDto>();
    }

    public async Task<InternalUserDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"internal/users/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InternalUserDto>(_jsonOptions, ct);
    }

    public async Task<InternalUserDto?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"internal/users/by-email/{Uri.EscapeDataString(email)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InternalUserDto>(_jsonOptions, ct);
    }

    public async Task<InternalUserDto> CreateAsync(InternalUserDto user, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("internal/users", user, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<InternalUserDto>(_jsonOptions, ct);
        return payload ?? user;
    }

    public async Task<InternalUserDto> UpdateAsync(int id, InternalUserDto user, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"internal/users/{id}", user, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<InternalUserDto>(_jsonOptions, ct);
        return payload ?? user;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"internal/users/{id}", ct);
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
