using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Models;
using Avatar_3D_Sentry.Settings;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Avatar_3D_Sentry.Services;

public class InternalApiAvatarDataStore : IAvatarDataStore
{
    private readonly InternalApiOptions _options;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly Uri _baseUri;
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private string? _token;
    private DateTimeOffset _tokenExpiresAtUtc = DateTimeOffset.MinValue;

    public InternalApiAvatarDataStore(IOptions<InternalApiOptions> options, HttpClient httpClient)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _baseUri = BuildBaseUri(_options.BaseUrl);
    }

    public async Task<ApplicationUser?> FindUserByEmailAsync(string email, CancellationToken ct)
    {
        var uri = $"internal/users/by-email/{Uri.EscapeDataString(email)}";
        return await GetOrNullAsync<ApplicationUser>(uri, ct);
    }

    public async Task<ApplicationUser?> FindUserByIdAsync(int id, CancellationToken ct)
    {
        var uri = $"internal/users/{id}";
        return await GetOrNullAsync<ApplicationUser>(uri, ct);
    }

    public async Task<(int total, List<ApplicationUser> items)> ListUsersAsync(int skip, int take, string? q, string? role, CancellationToken ct)
    {
        var page = Math.Max(1, (skip / Math.Max(1, take)) + 1);
        var pageSize = Math.Max(1, take);
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
            ["email"] = string.IsNullOrWhiteSpace(q) ? null : q,
            ["role"] = string.IsNullOrWhiteSpace(role) ? null : role
        };

        var uri = QueryHelpers.AddQueryString("internal/users", query);
        var response = await SendAsync(HttpMethod.Get, uri, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<UserListResponse>(_jsonOptions, ct);
        if (payload is null) return (0, new List<ApplicationUser>());

        return (payload.Total, payload.Items ?? new List<ApplicationUser>());
    }

    public async Task<bool> UserEmailExistsAsync(string email, CancellationToken ct)
    {
        var user = await FindUserByEmailAsync(email, ct);
        return user is not null;
    }

    public async Task<ApplicationUser> CreateUserAsync(ApplicationUser user, CancellationToken ct)
    {
        var response = await SendAsync(HttpMethod.Post, "internal/users", ct, user);
        response.EnsureSuccessStatusCode();

        var created = await FindUserByEmailAsync(user.Email, ct);
        return created ?? user;
    }

    public async Task UpdateUserAsync(ApplicationUser user, CancellationToken ct)
    {
        var response = await SendAsync(HttpMethod.Put, $"internal/users/{user.Id}", ct, user);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteUserAsync(ApplicationUser user, CancellationToken ct)
    {
        var response = await SendAsync(HttpMethod.Delete, $"internal/users/{user.Id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateUserPasswordHashAsync(int userId, string passwordHash, CancellationToken ct)
    {
        var user = await FindUserByIdAsync(userId, ct);
        if (user is null)
            throw new InvalidOperationException($"No se encontró usuario con Id={userId}.");

        user.PasswordHash = passwordHash;
        await UpdateUserAsync(user, ct);
    }

    public async Task<AvatarConfig?> FindAvatarConfigAsync(string empresa, string sede, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["empresa"] = empresa,
            ["sede"] = sede
        };

        var uri = QueryHelpers.AddQueryString("internal/avatar-config/by-scope", query);
        return await GetOrNullAsync<AvatarConfig>(uri, ct);
    }

    public async Task<AvatarConfig> CreateAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
    {
        var response = await SendAsync(HttpMethod.Post, "internal/avatar-config", ct, config);
        response.EnsureSuccessStatusCode();

        var created = await FindAvatarConfigAsync(config.Empresa, config.Sede, ct);
        return created ?? config;
    }

    public async Task UpdateAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
    {
        var response = await SendAsync(HttpMethod.Put, $"internal/avatar-config/{config.Id}", ct, config);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
    {
        var response = await SendAsync(HttpMethod.Delete, $"internal/avatar-config/{config.Id}", ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, CancellationToken ct, object? body = null)
    {
        var request = new HttpRequestMessage(method, BuildAbsoluteUri(uri));
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        await ApplyAuthHeadersAsync(request, ct);
        return await _httpClient.SendAsync(request, ct);
    }

    private async Task ApplyAuthHeadersAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await GetTokenAsync(ct);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.TryAddWithoutValidation("X-Api-Key", _options.ApiKey);
        }
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(_token) && _tokenExpiresAtUtc > now.AddMinutes(5))
        {
            return _token!;
        }

        await _authLock.WaitAsync(ct);
        try
        {
            if (!string.IsNullOrWhiteSpace(_token) && _tokenExpiresAtUtc > now.AddMinutes(5))
            {
                return _token!;
            }

            if (string.IsNullOrWhiteSpace(_options.AuthUser) || string.IsNullOrWhiteSpace(_options.AuthPassword))
            {
                throw new InvalidOperationException("Falta InternalApi:AuthUser o InternalApi:AuthPassword para autenticación.");
            }

            var payload = new AuthRequest(_options.AuthUser, _options.AuthPassword);
            var response = await _httpClient.PostAsJsonAsync(BuildAbsoluteUri("api/Token/Authentication"), payload, ct);
            response.EnsureSuccessStatusCode();

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions, ct);
            if (auth is null || string.IsNullOrWhiteSpace(auth.Token))
                throw new InvalidOperationException("La API interna no devolvió un token válido.");

            _token = auth.Token;
            _tokenExpiresAtUtc = ReadTokenExpiry(auth.Token) ?? now.AddHours(1);
            return _token;
        }
        finally
        {
            _authLock.Release();
        }
    }

    private DateTimeOffset? ReadTokenExpiry(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            if (jwt.ValidTo == DateTime.MinValue)
            {
                return null;
            }

            return new DateTimeOffset(jwt.ValidTo, TimeSpan.Zero);
        }
        catch
        {
            return null;
        }
    }

    private async Task<T?> GetOrNullAsync<T>(string uri, CancellationToken ct)
    {
        var response = await SendAsync(HttpMethod.Get, uri, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
    }

    private sealed record AuthRequest(string User, string Password);
    private sealed record AuthResponse(string Token);

    private sealed record UserListResponse(int Page, int PageSize, int Total, List<ApplicationUser>? Items);

    private Uri BuildAbsoluteUri(string relativeOrAbsolute)
    {
        if (Uri.TryCreate(relativeOrAbsolute, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        return new Uri(_baseUri, relativeOrAbsolute);
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
