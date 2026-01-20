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

        var payload = await ReadJsonOrDefaultAsync<PagedResponse<InternalUserDto>>(response, ct);
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
        return await ReadJsonOrDefaultAsync<InternalUserDto>(response, ct);
    }

    public async Task<InternalUserDto?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"internal/users/by-email/{Uri.EscapeDataString(email)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await ReadJsonOrDefaultAsync<InternalUserDto>(response, ct);
    }

    public async Task<InternalUserDto> CreateAsync(InternalUserDto user, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("internal/users", user, ct);
        response.EnsureSuccessStatusCode();

        var payload = await ReadJsonOrDefaultAsync<InternalUserDto>(response, ct);
        return payload ?? user;
    }

    public async Task<InternalUserDto> UpdateAsync(int id, InternalUserDto user, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"internal/users/{id}", user, ct);
        response.EnsureSuccessStatusCode();

        var payload = await ReadJsonOrDefaultAsync<InternalUserDto>(response, ct);
        return payload ?? user;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"internal/users/{id}", ct);

        if (response.IsSuccessStatusCode)
            return;

        var body = response.Content is null ? "" : await response.Content.ReadAsStringAsync(ct);

        // Para que el controller pueda mapear status code sin perder info
        throw new HttpRequestException(
            $"Internal API DELETE internal/users/{id} failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}",
            inner: null,
            statusCode: response.StatusCode
        );
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
}
