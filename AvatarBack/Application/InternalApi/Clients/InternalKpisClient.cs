using System.Net;
using AvatarSentry.Application.Config;
using Microsoft.Extensions.Options;

namespace AvatarSentry.Application.InternalApi.Clients;

public class InternalKpisClient : IInternalKpisClient
{
    private readonly HttpClient _httpClient;

    public InternalKpisClient(HttpClient httpClient, IOptions<InternalApiSettings> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = InternalAvatarConfigClient.BuildBaseUri(options.Value.BaseUrl);
    }

    public async Task<string?> GetGlobalAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("internal/kpis/global", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string?> GetByCompanyAsync(int companyId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"internal/kpis/company/{companyId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string?> GetBySiteAsync(int siteId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"internal/kpis/site/{siteId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
}
