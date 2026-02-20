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
        _httpClient.BaseAddress = InternalAvatarConfigClient.BuildBaseUri(options.Value.BaseUrl);
    }

    public async Task<PagedResponse<InternalUserDto>> GetUsersAsync(UserFilter filter, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["company"] = filter.Company.HasValue ? filter.Company.Value.ToString() : null,
            ["site"] = filter.Site.HasValue ? filter.Site.Value.ToString() : null,
            ["role"] = string.IsNullOrWhiteSpace(filter.Role) ? null : filter.Role,
            ["email"] = string.IsNullOrWhiteSpace(filter.Email) ? null : filter.Email,
            ["page"] = Math.Max(filter.Page, 1).ToString(),
            ["pageSize"] = Math.Max(filter.PageSize, 1).ToString()
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
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.InternalServerError)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await ReadJsonOrDefaultAsync<InternalUserDto>(response, ct);
    }

    public async Task<InternalUserByEmailDto?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"internal/users/by-email/{Uri.EscapeDataString(email)}", ct);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.InternalServerError)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await ReadJsonOrDefaultAsync<InternalUserByEmailDto>(response, ct);
    }

    public async Task<InternalUserDto> CreateAsync(CreateInternalUserRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("internal/users", request, ct);
        response.EnsureSuccessStatusCode();

        var created = await GetByEmailAsync(request.Email, ct);
        if (created is not null)
            return MapByEmailToDto(created);
        return new InternalUserDto
        {
            Email = request.Email,
            FullName = request.FullName ?? string.Empty,
            Role = request.Role,
            CompanyId = request.CompanyId,
            SiteId = request.SiteId,
            IsActive = request.IsActive
        };
    }

    public async Task<InternalUserDto> UpdateAsync(int id, UpdateInternalUserRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"internal/users/{id}", request, ct);
        response.EnsureSuccessStatusCode();

        var refreshed = await GetByIdAsync(id, ct);
        if (refreshed is not null)
            return refreshed;
        return new InternalUserDto
        {
            Id = id,
            Email = request.Email,
            FullName = request.FullName ?? string.Empty,
            Role = request.Role,
            CompanyId = request.CompanyId,
            SiteId = request.SiteId,
            IsActive = request.IsActive
        };
    }

    public async Task ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"internal/users/{id}/reset-password", request, ct);
        response.EnsureSuccessStatusCode();
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

    private static InternalUserDto MapByEmailToDto(InternalUserByEmailDto byEmail)
    {
        return new InternalUserDto
        {
            Id = byEmail.Id,
            Email = byEmail.Email,
            FullName = byEmail.FullName ?? string.Empty,
            Role = byEmail.Role,
            CompanyId = byEmail.CompanyId,
            SiteId = byEmail.SiteId,
            IsActive = byEmail.IsActive
        };
    }
}
