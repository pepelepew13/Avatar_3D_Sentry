using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AvatarAdmin.Models;
using Microsoft.Extensions.Options;

namespace AvatarAdmin.Services;

public class AvatarApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AvatarApiClient> _logger;
    private readonly ApiOptions _options;

    public AvatarApiClient(HttpClient httpClient, IOptions<ApiOptions> options, ILogger<AvatarApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;

        if (_httpClient.BaseAddress is null && !string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        }
    }

    public Uri? BaseAddress => _httpClient.BaseAddress;

    public async Task<AvatarConfigDto> GetConfigAsync(string empresa, string sede, CancellationToken cancellationToken = default)
    {
        var requestUri = $"api/avatar/config?empresa={Uri.EscapeDataString(empresa)}&sede={Uri.EscapeDataString(sede)}";
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var config = await response.Content.ReadFromJsonAsync<AvatarConfigDto>(cancellationToken: cancellationToken);
        return config ?? throw new InvalidOperationException("La respuesta no contiene una configuración válida.");
    }

    public async Task<AvatarConfigDto> UpdateConfigAsync(string empresa, string sede, AvatarConfigUpdate update, CancellationToken cancellationToken = default)
    {
        var requestUri = $"AvatarEditor/{Uri.EscapeDataString(empresa)}/{Uri.EscapeDataString(sede)}";
        using var response = await _httpClient.PostAsJsonAsync(requestUri, update, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var config = await response.Content.ReadFromJsonAsync<AvatarConfigDto>(cancellationToken: cancellationToken);
        return config ?? throw new InvalidOperationException("No fue posible leer la configuración actualizada.");
    }

    public async Task<string?> UploadLogoAsync(string empresa, string sede, Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var requestUri = $"AvatarEditor/{Uri.EscapeDataString(empresa)}/{Uri.EscapeDataString(sede)}/logo";
        using var form = new MultipartFormDataContent();
        var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        form.Add(content, "logo", fileName);

        using var response = await _httpClient.PostAsync(requestUri, form, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        if (doc.RootElement.TryGetProperty("logoPath", out var value))
        {
            return value.GetString();
        }

        return null;
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetVoicesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("AvatarEditor/voces", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, Dictionary<string, List<string>>>>(cancellationToken: cancellationToken);
        if (payload is null || payload.Count == 0)
        {
            return new Dictionary<string, IReadOnlyList<string>>();
        }

        var voices = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in payload.Values)
        {
            foreach (var kvp in provider)
            {
                voices[kvp.Key] = kvp.Value;
            }
        }

        return voices;
    }

    public string? BuildAssetUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute))
        {
            return absolute.ToString();
        }

        if (_httpClient.BaseAddress is null)
        {
            return path;
        }

        return new Uri(_httpClient.BaseAddress, path.TrimStart('/')).ToString();
    }

    public Task<AnnouncementResponse> RequestAnnouncementPreviewAsync(
        string empresa,
        string sede,
        string? idioma,
        CancellationToken cancellationToken = default)
    {
        var payload = new AnnouncementRequest
        {
            Empresa = empresa,
            Sede = sede,
            Modulo = "Módulo demo",
            Turno = "A001",
            Nombre = "Cliente demo"
        };

        return AnunciarAsync(payload, idioma, cancellationToken);
    }

    public Task<AnnouncementResponse> AnunciarAsync(
        AnnouncementRequest request,
        string? idioma,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return SendAnnouncementAsync(request, idioma, cancellationToken);
    }

    public Task<AnnouncementResponse> AnunciarAsync(
        string empresa,
        string sede,
        string modulo,
        string turno,
        string nombre,
        string? idioma,
        CancellationToken cancellationToken = default)
    {
        var payload = new AnnouncementRequest
        {
            Empresa = empresa,
            Sede = sede,
            Modulo = modulo,
            Turno = turno,
            Nombre = nombre
        };

        return SendAnnouncementAsync(payload, idioma, cancellationToken);
    }

    private async Task<AnnouncementResponse> SendAnnouncementAsync(
        AnnouncementRequest payload,
        string? idioma,
        CancellationToken cancellationToken)
    {
        var requestUri = string.IsNullOrWhiteSpace(idioma)
            ? "Avatar/anunciar"
            : $"Avatar/anunciar?idioma={Uri.EscapeDataString(idioma)}";

        using var response = await _httpClient.PostAsJsonAsync(requestUri, payload, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var announcement = await response.Content.ReadFromJsonAsync<AnnouncementResponse>(cancellationToken: cancellationToken);
        return announcement ?? throw new InvalidOperationException("La API no devolvió un anuncio válido.");
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning("La API devolvió un error {StatusCode}: {Body}", (int)response.StatusCode, body);
        throw new InvalidOperationException($"La API devolvió un estado {(int)response.StatusCode}. Detalle: {body}");
    }

    public async Task<string?> UploadBackgroundAsync(
        string empresa,
        string sede,
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // Ajusta esta ruta si en tu API usaste otra (p. ej. ".../fondo").
        var requestUri = $"AvatarEditor/{Uri.EscapeDataString(empresa)}/{Uri.EscapeDataString(sede)}/background";

        using var form = new MultipartFormDataContent();

        // Campo del formulario: usa "background" (si tu API espera "fondo", cambia el primer parámetro)
        var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        form.Add(content, "background", fileName);

        using var response = await _httpClient.PostAsync(requestUri, form, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        // Acepta varias claves posibles desde el backend
        if (doc.RootElement.TryGetProperty("backgroundPath", out var v1))
            return v1.GetString();
        if (doc.RootElement.TryGetProperty("fondoPath", out var v2))
            return v2.GetString();
        if (doc.RootElement.TryGetProperty("path", out var v3))
            return v3.GetString();
        if (doc.RootElement.TryGetProperty("url", out var v4))
            return v4.GetString();

        return null;
    }
}
