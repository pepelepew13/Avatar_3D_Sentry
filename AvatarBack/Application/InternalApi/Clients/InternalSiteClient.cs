using System.Net;
using System.Text.Json;
using AvatarSentry.Application.Config;
using AvatarSentry.Application.InternalApi.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace AvatarSentry.Application.InternalApi.Clients;

public class InternalSiteClient : IInternalSiteClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public InternalSiteClient(HttpClient httpClient, IOptions<InternalApiSettings> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = InternalAvatarConfigClient.BuildBaseUri(options.Value.BaseUrl);
    }

    public async Task<InternalSiteDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"internal/sites/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await ReadJsonOrDefaultAsync<InternalSiteDto>(response, ct);
    }

    public async Task<InternalSiteDto?> GetByCompanyAndCodeAsync(int companyId, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;
        var query = new Dictionary<string, string?>
        {
            ["companyId"] = companyId.ToString(),
            ["code"] = code.Trim(),
            ["page"] = "1",
            ["pageSize"] = "1"
        };
        var uri = QueryHelpers.AddQueryString("internal/sites", query);
        var response = await _httpClient.GetAsync(uri, ct);
        response.EnsureSuccessStatusCode();
        var payload = await ReadJsonOrDefaultAsync<InternalSitePagedResponse>(response, ct);
        var item = payload?.Items?.FirstOrDefault();
        if (item is null)
            return null;
        return string.Equals(item.Code, code.Trim(), StringComparison.OrdinalIgnoreCase) ? item : null;
    }

    public async Task<PagedResponse<InternalSiteDto>> GetSitesAsync(int? companyId, string? code, string? name, bool? isActive, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["companyId"] = companyId.HasValue ? companyId.Value.ToString() : null,
            ["code"] = string.IsNullOrWhiteSpace(code) ? null : code,
            ["name"] = string.IsNullOrWhiteSpace(name) ? null : name,
            ["isActive"] = isActive.HasValue ? isActive.Value.ToString().ToLowerInvariant() : null,
            ["page"] = Math.Max(1, page).ToString(),
            ["pageSize"] = Math.Max(1, pageSize).ToString()
        };
        var uri = QueryHelpers.AddQueryString("internal/sites", query);
        var response = await _httpClient.GetAsync(uri, ct);
        response.EnsureSuccessStatusCode();
        var payload = await ReadJsonOrDefaultAsync<InternalSitePagedResponse>(response, ct);
        if (payload is null)
            return new PagedResponse<InternalSiteDto>();
        return new PagedResponse<InternalSiteDto>
        {
            Page = payload.Page,
            PageSize = payload.PageSize,
            Total = payload.Total,
            TotalPages = payload.TotalPages,
            Items = payload.Items ?? new List<InternalSiteDto>()
        };
    }

    private async Task<T?> ReadJsonOrDefaultAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.Content is null) return default;
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(raw)) return default;
        return JsonSerializer.Deserialize<T>(raw, _jsonOptions);
    }
}
