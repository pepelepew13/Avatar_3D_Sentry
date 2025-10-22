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
        var resp = await _http.PostAsJsonAsync("/api/auth/users", req, _json, ct);
        await EnsureSuccessAsync(resp, ct);
    }

    // ========= Avatar Config =========

    public async Task<AvatarConfigDto> GetConfigAsync(string empresa, string sede, CancellationToken ct = default)
    {
        var url = $"/api/avatar/{Uri.EscapeDataString(empresa)}/{Uri.EscapeDataString(sede)}/config";
        var resp = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<AvatarConfigDto>(_json, ct))!;
    }

    public async Task<AvatarConfigDto> UpdateConfigAsync(string empresa, string sede, AvatarConfigUpdate update, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var url = $"/api/avatar/{Uri.EscapeDataString(empresa)}/{Uri.EscapeDataString(sede)}";
        var resp = await _http.PutAsJsonAsync(url, update, _json, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<AvatarConfigDto>(_json, ct))!;
    }

    public async Task<AvatarConfigDto> UploadLogoAsync(string empresa, string sede, Stream file, string fileName, string contentType, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        content.Add(fileContent, "file", fileName);

        var url = $"/api/avatar/{Uri.EscapeDataString(empresa)}/{Uri.EscapeDataString(sede)}/logo";
        var resp = await _http.PostAsync(url, content, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<AvatarConfigDto>(_json, ct))!;
    }

    public async Task<AvatarConfigDto> UploadBackgroundAsync(string empresa, string sede, Stream file, string fileName, string contentType, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        content.Add(fileContent, "file", fileName);

        var url = $"/api/avatar/{Uri.EscapeDataString(empresa)}/{Uri.EscapeDataString(sede)}/background";
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

    public async Task<ModelsResponse> GetModelsAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync("/api/models", ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<ModelsResponse>(_json, ct))!;
    }

    public async Task<AnnouncementResponse> AnnounceAsync(AnnouncementRequest req, string idioma, string voz, CancellationToken ct = default)
    {
        var url = $"/api/Avatar/announce?idioma={Uri.EscapeDataString(idioma)}&voz={Uri.EscapeDataString(voz)}";
        var resp = await _http.PostAsJsonAsync(url, req, _json, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<AnnouncementResponse>(_json, ct))!;
    }

    public string BaseUrl => _http.BaseAddress?.ToString() ?? "/";

    // ========= NUEVO: Gestión de usuarios (lista/edita/borra) =========

    public sealed class UserItem
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string? Empresa { get; set; }
        public string? Sede { get; set; }
    }

    public sealed class UserListResponse
    {
        public int Total { get; set; }
        public List<UserItem> Items { get; set; } = new();
    }

    public sealed class UpdateUserRequest
    {
        public string Role { get; set; } = "User";
        public string? Empresa { get; set; }
        public string? Sede { get; set; }
        public string? NewPassword { get; set; }
    }

    /// <summary>GET /api/auth/users?skip=0&take=10&q=...&role=User|Admin</summary>
    public async Task<UserListResponse> ListUsersAsync(int skip = 0, int take = 10, string? q = null, string? role = null, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var qs = new List<string> { $"skip={skip}", $"take={take}" };
        if (!string.IsNullOrWhiteSpace(q)) qs.Add("q=" + Uri.EscapeDataString(q));
        if (!string.IsNullOrWhiteSpace(role)) qs.Add("role=" + Uri.EscapeDataString(role));
        var url = "/api/auth/users" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        var resp = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(resp, ct);
        return (await resp.Content.ReadFromJsonAsync<UserListResponse>(_json, ct))!;
    }

    /// <summary>PUT /api/auth/users/{id}</summary>
    public async Task UpdateUserAsync(string id, UpdateUserRequest req, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var url = "/api/auth/users/" + Uri.EscapeDataString(id);
        var resp = await _http.PutAsJsonAsync(url, req, _json, ct);
        await EnsureSuccessAsync(resp, ct);
    }

    /// <summary>DELETE /api/auth/users/{id}</summary>
    public async Task DeleteUserAsync(string id, CancellationToken ct = default)
    {
        AttachBearerIfAny();
        var url = "/api/auth/users/" + Uri.EscapeDataString(id);
        var resp = await _http.DeleteAsync(url, ct);
        await EnsureSuccessAsync(resp, ct);
    }
}
