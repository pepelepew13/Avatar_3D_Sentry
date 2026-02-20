using System.Net.Http.Json;
using System.Text.Json;
using AvatarSentry.Application.Config;
using Microsoft.Extensions.Options;

namespace AvatarSentry.Application.InternalApi;

public class InternalApiTokenService : IInternalApiTokenService
{
    private readonly HttpClient _httpClient;
    private readonly InternalApiSettings _settings;
    private readonly SemaphoreSlim _authLock = new(1, 1);
    /// <summary>La API interna espera "User" y "Password" en PascalCase (documentación UserAvatarApi).</summary>
    private static readonly JsonSerializerOptions AuthRequestOptions = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = false,
        WriteIndented = false
    };
    private static readonly JsonSerializerOptions AuthResponseOptions = new() { PropertyNameCaseInsensitive = true };
    private string? _cachedToken;

    public InternalApiTokenService(HttpClient httpClient, IOptions<InternalApiSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _httpClient.BaseAddress = BuildBaseUri(_settings.BaseUrl);
    }

    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(_cachedToken))
        {
            return _cachedToken;
        }

        await _authLock.WaitAsync(ct);
        try
        {
            if (!string.IsNullOrWhiteSpace(_cachedToken))
            {
                return _cachedToken;
            }

            if (string.IsNullOrWhiteSpace(_settings.AuthUser) || string.IsNullOrWhiteSpace(_settings.AuthPassword))
            {
                throw new InvalidOperationException("Falta InternalApi:AuthUser o InternalApi:AuthPassword para autenticación.");
            }

            var payload = new AuthRequest(_settings.AuthUser, _settings.AuthPassword);
            var response = await _httpClient.PostAsJsonAsync("api/Token/Authentication", payload, AuthRequestOptions, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException(
                    $"La API interna rechazó la autenticación ({(int)response.StatusCode}). " +
                    "Compruebe InternalApi:BaseUrl, InternalApi:AuthUser e InternalApi:AuthPassword. " +
                    "User y Password deben ser los valores con encriptación del comconfig. Respuesta: " + (string.IsNullOrEmpty(body) ? response.ReasonPhrase : body));
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(AuthResponseOptions, ct);
            if (auth is null || string.IsNullOrWhiteSpace(auth.Token))
            {
                throw new InvalidOperationException("La API interna no devolvió un token válido.");
            }

            _cachedToken = auth.Token;
            return _cachedToken;
        }
        finally
        {
            _authLock.Release();
        }
    }

    public void InvalidateToken()
    {
        _cachedToken = null;
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

    private sealed record AuthRequest(string User, string Password);
    private sealed record AuthResponse(string Token);
}
