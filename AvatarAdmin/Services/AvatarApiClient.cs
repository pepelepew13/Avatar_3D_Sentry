using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AvatarAdmin.Models;

namespace AvatarAdmin.Services;

public sealed class AvatarApiClient
{
    private readonly HttpClient _http;
    private readonly AuthState _auth;
    private readonly JsonSerializerOptions _json;

    public AvatarApiClient(HttpClient http, AuthState auth)
    {
        _http = http;
        _auth = auth;

        _json = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
    }

    // ========= Helpers =========

    private void AttachBearerIfAny()
    {
        if (!string.IsNullOrWhiteSpace(_auth.Token))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _auth.Token);
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        if (resp.IsSuccessStatusCode) return;

        var body = await resp.Content.ReadAsStringAsync(ct);
        var msg = $"La API devolvió un estado {(int)resp.StatusCode}. Detalle: {body}";
        throw new InvalidOperationException(msg);
    }

    // === JWT helpers ===
    private static DateTime? TryGetJwtExpiryUtc(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;

            string payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("exp", out var expProp) && expProp.TryGetInt64(out var exp))
            {
                return DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static byte[] Base64UrlDecode(string base64Url)
    {
        string s = base64Url.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }

    // ========= Auth =========

    public async Task<LoginResponse> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        _http.DefaultRequestHeaders.Authorization = null;

        var resp = await _http.PostAsJsonAsync("/api/auth/login", req, _json, ct);
        await EnsureSuccessAsync(resp, ct);

        var payload = await resp.Content.ReadFromJsonAsync<LoginResponse>(_json, ct)
                      ?? throw new InvalidOperationException("Respuesta login vacía.");

        var token = (payload.Token ?? string.Empty).Trim();

        DateTime? expires = payload.ExpiresAtUtc;

        if (expires is null || expires <= DateTime.UtcNow.AddMinutes(1))
        {
            var fromJwt = !string.IsNullOrWhiteSpace(token) ? TryGetJwtExpiryUtc(token) : null;
            if (fromJwt is not null) expires = fromJwt;
            if (expires is null || expires <= DateTime.UtcNow.AddMinutes(1))
                expires = DateTime.UtcNow.AddHours(8);
        }

        _auth.SetSession(
            token: token,
            expiresAtUtc: expires.Value,
            email: req.Email,
            role: payload.Role,
            empresa: payload.Empresa,
            sede: payload.Sede
        );

        return payload;
    }

    public async Task CreateUserAsync(CreateUserRequest req, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var resp = await _http.PostAsJsonAsync("/api/users", req, _json, ct);
        await EnsureSuccessAsync(resp, ct);
    }

    // ========= Avatar Config =========

    public sealed class AvatarConfigListResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<AvatarConfigDto> Items { get; set; } = new();
    }

    public sealed class AvatarConfigRequest
    {
        public string Empresa { get; set; } = string.Empty;
        public string Sede { get; set; } = string.Empty;
        public string? Vestimenta { get; set; }
        public string? Fondo { get; set; }
        public string? Voz { get; set; }
        public string? Idioma { get; set; }
        public string? LogoPath { get; set; }
        public string? ColorCabello { get; set; }
        public string? BackgroundPath { get; set; }
    }

    public async Task<AvatarConfigDto?> GetConfigAsync(string empresa, string sede, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var qs = new List<string>
        {
            "empresa=" + Uri.EscapeDataString(empresa),
            "sede=" + Uri.EscapeDataString(sede),
            "page=1",
            "pageSize=1"
        };
        var url = "/api/avatar-configs?" + string.Join("&", qs);
        var resp = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(resp, ct);

        var payload = await resp.Content.ReadFromJsonAsync<AvatarConfigListResponse>(_json, ct);
        return payload?.Items?.FirstOrDefault();
    }

    public async Task<AvatarConfigDto?> GetPublicConfigAsync(string empresa, string sede, CancellationToken ct = default)
    {
        var qs = new List<string>
        {
            "empresa=" + Uri.EscapeDataString(empresa),
            "sede=" + Uri.EscapeDataString(sede)
        };
        var url = "/api/avatar/config?" + string.Join("&", qs);
        var resp = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(resp, ct);
        return await resp.Content.ReadFromJsonAsync<AvatarConfigDto>(_json, ct);
    }

    public async Task<AvatarConfigListResponse> ListConfigsAsync(string? empresa = null, string? sede = null, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var qs = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(empresa)) qs.Add("empresa=" + Uri.EscapeDataString(empresa));
        if (!string.IsNullOrWhiteSpace(sede)) qs.Add("sede=" + Uri.EscapeDataString(sede));
        var url = "/api/avatar-configs" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        var resp = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<AvatarConfigListResponse>(_json, ct))!;
    }

    public async Task<AvatarConfigDto> CreateConfigAsync(AvatarConfigRequest req, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var resp = await _http.PostAsJsonAsync("/api/avatar-configs", req, _json, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<AvatarConfigDto>(_json, ct))!;
    }

    public async Task<AvatarConfigDto> UpdateConfigAsync(int id, AvatarConfigRequest update, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var url = $"/api/avatar-configs/{id}";
        var resp = await _http.PutAsJsonAsync(url, update, _json, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<AvatarConfigDto>(_json, ct))!;
    }

    public async Task<AvatarConfigDto> UploadLogoAsync(int id, Stream file, string fileName, string contentType, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        content.Add(fileContent, "file", fileName);

        var url = $"/api/avatar-configs/{id}/logo";
        var resp = await _http.PostAsync(url, content, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<AvatarConfigDto>(_json, ct))!;
    }

    public async Task<AvatarConfigDto> UploadBackgroundAsync(int id, Stream file, string fileName, string contentType, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        content.Add(fileContent, "file", fileName);

        var url = $"/api/avatar-configs/{id}/fondo";
        var resp = await _http.PostAsync(url, content, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<AvatarConfigDto>(_json, ct))!;
    }

    public async Task<Dictionary<string, List<string>>> GetVoicesAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync("/api/tts/voices", ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<Dictionary<string, List<string>>>(_json, ct))!;
    }

    public sealed class ModelsResponse
    {
        public string BaseUrl { get; set; } = string.Empty;
        public List<string> Files { get; set; } = new();
    }

    public sealed class AssetUrlResponse
    {
        public string Path { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public async Task<ModelsResponse> GetModelsAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync("/api/models", ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<ModelsResponse>(_json, ct))!;
    }

    public async Task<string?> GetAssetUrlAsync(string path, int? ttlSeconds = null, CancellationToken ct = default)
    {
        var qs = new List<string> { "path=" + Uri.EscapeDataString(path) };
        if (ttlSeconds is not null) qs.Add($"ttlSeconds={ttlSeconds.Value}");

        var url = "/api/assets/url?" + string.Join('&', qs);
        var resp = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(resp, ct);

        var payload = await resp.Content.ReadFromJsonAsync<AssetUrlResponse>(_json, ct);
        return payload?.Url ?? payload?.Path;
    }

    public async Task<AnnouncementResponse> AnnounceAsync(AnnouncementRequest req, string idioma, string voz, CancellationToken ct = default)
    {
        var url = $"/api/Avatar/announce?idioma={Uri.EscapeDataString(idioma)}&voz={Uri.EscapeDataString(voz)}";
        var resp = await _http.PostAsJsonAsync(url, req, _json, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<AnnouncementResponse>(_json, ct))!;
    }

    public async Task<AvatarConfigDto> UploadOutfitAsync(int id, Stream file, string fileName, string contentType, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        content.Add(fileContent, "file", fileName);

        var url = $"/api/avatar-configs/{id}/model";
        var resp = await _http.PostAsync(url, content, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<AvatarConfigDto>(_json, ct))!;
    }

    public string BaseUrl => _http.BaseAddress?.ToString() ?? "/";

    // ========= NUEVO: Gestión de usuarios (lista/edita/borra) =========

    public sealed class UserItem
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string? Empresa { get; set; }
        public string? Sede { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class UserListResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<UserItem> Items { get; set; } = new();
    }

    public sealed class UpdateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string Role { get; set; } = "User";
        public string? Empresa { get; set; }
        public string? Sede { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>GET /api/users?page=1&pageSize=10&q=...&role=...&empresa=...&sede=...</summary>
    public async Task<UserListResponse> ListUsersAsync(int page = 1, int pageSize = 10, string? q = null, string? role = null, string? empresa = null, string? sede = null, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var qs = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(q)) qs.Add("q=" + Uri.EscapeDataString(q));
        if (!string.IsNullOrWhiteSpace(role)) qs.Add("role=" + Uri.EscapeDataString(role));
        if (!string.IsNullOrWhiteSpace(empresa)) qs.Add("empresa=" + Uri.EscapeDataString(empresa));
        if (!string.IsNullOrWhiteSpace(sede)) qs.Add("sede=" + Uri.EscapeDataString(sede));
        var url = "/api/users?" + string.Join("&", qs);
        var resp = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<UserListResponse>(_json, ct))!;
    }

    /// <summary>PUT /api/users/{id}</summary>
    public async Task UpdateUserAsync(int id, UpdateUserRequest req, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var url = "/api/users/" + Uri.EscapeDataString(id.ToString());
        var resp = await _http.PutAsJsonAsync(url, req, _json, ct);
        await EnsureSuccessAsync(resp, ct);
    }

    /// <summary>DELETE /api/users/{id}</summary>
    public async Task DeleteUserAsync(int id, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var url = "/api/users/" + Uri.EscapeDataString(id.ToString());
        var resp = await _http.DeleteAsync(url, ct);
        await EnsureSuccessAsync(resp, ct);
    }

    // ========= Companies (solo Admin) =========

    public sealed class CompanyItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? CorporateId { get; set; }
        public string? Sector { get; set; }
        public string? LogoPath { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class CompanyListResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
        public List<CompanyItem> Items { get; set; } = new();
    }

    public async Task<CompanyListResponse> ListCompaniesAsync(string? code = null, string? name = null, string? sector = null, bool? isActive = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var qs = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(code)) qs.Add("code=" + Uri.EscapeDataString(code));
        if (!string.IsNullOrWhiteSpace(name)) qs.Add("name=" + Uri.EscapeDataString(name));
        if (!string.IsNullOrWhiteSpace(sector)) qs.Add("sector=" + Uri.EscapeDataString(sector));
        if (isActive.HasValue) qs.Add("isActive=" + isActive.Value.ToString().ToLowerInvariant());
        var url = "/api/companies?" + string.Join("&", qs);
        var resp = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<CompanyListResponse>(_json, ct))!;
    }

    public async Task<CompanyItem?> GetCompanyAsync(int id, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var resp = await _http.GetAsync($"/api/companies/{id}", ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccessAsync(resp, ct);
        return await resp.Content.ReadFromJsonAsync<CompanyItem>(_json, ct);
    }

    // ========= Sites (solo Admin) =========

    public sealed class SiteItem
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class SiteListResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
        public List<SiteItem> Items { get; set; } = new();
    }

    public async Task<SiteListResponse> ListSitesAsync(int? companyId = null, string? code = null, string? name = null, bool? isActive = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var qs = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (companyId.HasValue) qs.Add("companyId=" + companyId.Value);
        if (!string.IsNullOrWhiteSpace(code)) qs.Add("code=" + Uri.EscapeDataString(code));
        if (!string.IsNullOrWhiteSpace(name)) qs.Add("name=" + Uri.EscapeDataString(name));
        if (isActive.HasValue) qs.Add("isActive=" + isActive.Value.ToString().ToLowerInvariant());
        var url = "/api/sites?" + string.Join("&", qs);
        var resp = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<SiteListResponse>(_json, ct))!;
    }

    public async Task<SiteItem?> GetSiteAsync(int id, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var resp = await _http.GetAsync($"/api/sites/{id}", ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccessAsync(resp, ct);
        return await resp.Content.ReadFromJsonAsync<SiteItem>(_json, ct);
    }

    // ========= KPIs (solo Admin) =========

    public async Task<string> GetKpisGlobalAsync(CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var resp = await _http.GetAsync("/api/kpis/global", ct);
        await EnsureSuccessAsync(resp, ct);
        return await resp.Content.ReadAsStringAsync(ct) ?? "{}";
    }

    public async Task<string> GetKpisCompanyAsync(int companyId, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var resp = await _http.GetAsync($"/api/kpis/company/{companyId}", ct);
        await EnsureSuccessAsync(resp, ct);
        return await resp.Content.ReadAsStringAsync(ct) ?? "{}";
    }

    public async Task<string> GetKpisSiteAsync(int siteId, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var resp = await _http.GetAsync($"/api/kpis/site/{siteId}", ct);
        await EnsureSuccessAsync(resp, ct);
        return await resp.Content.ReadAsStringAsync(ct) ?? "{}";
    }
}
