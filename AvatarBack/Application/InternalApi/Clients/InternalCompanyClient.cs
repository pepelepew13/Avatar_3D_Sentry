using System.Net;
using System.Text.Json;
using AvatarSentry.Application.Config;
using AvatarSentry.Application.InternalApi.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace AvatarSentry.Application.InternalApi.Clients;

public class InternalCompanyClient : IInternalCompanyClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public InternalCompanyClient(HttpClient httpClient, IOptions<InternalApiSettings> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = InternalAvatarConfigClient.BuildBaseUri(options.Value.BaseUrl);
    }

    public async Task<InternalCompanyDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"internal/companies/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await ReadJsonOrDefaultAsync<InternalCompanyDto>(response, ct);
    }

    public async Task<InternalCompanyDto?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;
        var query = new Dictionary<string, string?>
        {
            ["code"] = code.Trim(),
            ["page"] = "1",
            ["pageSize"] = "1"
        };
        var uri = QueryHelpers.AddQueryString("internal/companies", query);
        var response = await _httpClient.GetAsync(uri, ct);
        response.EnsureSuccessStatusCode();
        var payload = await ReadJsonOrDefaultAsync<InternalCompanyPagedResponse>(response, ct);
        var item = payload?.Items?.FirstOrDefault();
        if (item is null)
            return null;
        return string.Equals(item.Code, code.Trim(), StringComparison.OrdinalIgnoreCase) ? item : null;
    }

    public async Task<PagedResponse<InternalCompanyDto>> GetCompaniesAsync(string? code, string? name, string? sector, bool? isActive, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["code"] = string.IsNullOrWhiteSpace(code) ? null : code,
            ["name"] = string.IsNullOrWhiteSpace(name) ? null : name,
            ["sector"] = string.IsNullOrWhiteSpace(sector) ? null : sector,
            ["isActive"] = isActive.HasValue ? isActive.Value.ToString().ToLowerInvariant() : null,
            ["page"] = Math.Max(1, page).ToString(),
            ["pageSize"] = Math.Max(1, pageSize).ToString()
        };
        var uri = QueryHelpers.AddQueryString("internal/companies", query);
        var response = await _httpClient.GetAsync(uri, ct);
        response.EnsureSuccessStatusCode();
        var payload = await ReadJsonOrDefaultAsync<InternalCompanyPagedResponse>(response, ct);
        if (payload is null)
            return new PagedResponse<InternalCompanyDto>();
        return new PagedResponse<InternalCompanyDto>
        {
            Page = payload.Page,
            PageSize = payload.PageSize,
            Total = payload.Total,
            TotalPages = payload.TotalPages,
            Items = payload.Items ?? new List<InternalCompanyDto>()
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
