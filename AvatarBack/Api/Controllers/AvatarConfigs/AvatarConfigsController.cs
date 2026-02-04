using System.IO;
using Avatar_3D_Sentry.Models;
using Avatar_3D_Sentry.Services.Storage;
using AvatarSentry.Application.AvatarConfigs;
using AvatarSentry.Application.Exceptions;
using AvatarSentry.Application.InternalApi.Clients;
using AvatarSentry.Application.InternalApi.Models;
using Avatar_3D_Sentry.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/avatar-configs")]
[Authorize(Roles = "Admin")]
public class AvatarConfigsController : ControllerBase
{
    private readonly IInternalAvatarConfigClient _internalAvatarConfigClient;
    private readonly IAssetStorage _storage;
    private readonly AzureStorageOptions _storageOptions;

    public AvatarConfigsController(
        IInternalAvatarConfigClient internalAvatarConfigClient,
        IAssetStorage storage,
        IOptions<AzureStorageOptions> storageOptions)
    {
        _internalAvatarConfigClient = internalAvatarConfigClient;
        _storage = storage;
        _storageOptions = storageOptions.Value;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AvatarConfigListItemDto>>> GetConfigs(
        [FromQuery] string? empresa,
        [FromQuery] string? sede,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var filter = new AvatarConfigFilter
        {
            Empresa = string.IsNullOrWhiteSpace(empresa) ? null : empresa,
            Sede = string.IsNullOrWhiteSpace(sede) ? null : sede,
            Page = page,
            PageSize = pageSize
        };

        try
        {
            var result = await _internalAvatarConfigClient.GetConfigsAsync(filter, ct);
            var response = new PagedResult<AvatarConfigListItemDto>
            {
                Page = result.Page,
                PageSize = result.PageSize,
                Total = result.Total,
                Items = result.Items.Select(MapToListItemWithUrls).ToList()
            };

            return Ok(response);
        }
        catch (AvatarSentryException ex)
        {
            return Problem(
                detail: ex.Details,
                title: ex.Message,
                statusCode: ex.StatusCode);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AvatarConfigDto>> GetById(int id, CancellationToken ct)
    {
        var config = await _internalAvatarConfigClient.GetByIdAsync(id, ct);
        if (config is null)
        {
            return NotFound();
        }

        return Ok(MapToDtoWithUrls(config));
    }

    [HttpPost]
    public async Task<ActionResult<AvatarConfigDto>> Create([FromBody] CreateAvatarConfigRequest request, CancellationToken ct)
    {
        var payload = new InternalAvatarConfigDto
        {
            Empresa = request.Empresa,
            Sede = request.Sede,
            Vestimenta = request.Vestimenta,
            Fondo = request.Fondo,
            Voz = request.Voz,
            Idioma = request.Idioma,
            LogoPath = request.LogoPath,
            ColorCabello = request.ColorCabello,
            BackgroundPath = request.BackgroundPath,
            IsActive = true
        };

        try
        {
            var created = await _internalAvatarConfigClient.CreateAsync(payload, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDtoWithUrls(created));
        }
        catch (AvatarSentryException ex)
        {
            return Problem(
                detail: ex.Details,
                title: ex.Message,
                statusCode: ex.StatusCode);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AvatarConfigDto>> Update(int id, [FromBody] UpdateAvatarConfigRequest request, CancellationToken ct)
    {
        var existing = await _internalAvatarConfigClient.GetByIdAsync(id, ct);
        if (existing is null)
        {
            return NotFound();
        }

        var payload = new InternalAvatarConfigDto
        {
            Id = existing.Id,
            Empresa = request.Empresa,
            Sede = request.Sede,
            Vestimenta = request.Vestimenta,
            Fondo = request.Fondo,
            Voz = request.Voz,
            Idioma = request.Idioma,
            LogoPath = request.LogoPath,
            ColorCabello = request.ColorCabello,
            BackgroundPath = request.BackgroundPath,
            IsActive = existing.IsActive
        };

        try
        {
            var updated = await _internalAvatarConfigClient.UpdateAsync(id, payload, ct);
            return Ok(MapToDtoWithUrls(updated));
        }
        catch (AvatarSentryException ex)
        {
            return Problem(
                detail: ex.Details,
                title: ex.Message,
                statusCode: ex.StatusCode);
        }
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<AvatarConfigDto>> Patch(int id, [FromBody] AvatarConfigPatchRequest request, CancellationToken ct)
    {
        var existing = await _internalAvatarConfigClient.GetByIdAsync(id, ct);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Vestimenta = request.Vestimenta ?? existing.Vestimenta;
        existing.Fondo = request.Fondo ?? existing.Fondo;
        existing.Voz = request.Voz ?? existing.Voz;
        existing.Idioma = request.Idioma ?? existing.Idioma;
        existing.LogoPath = request.LogoPath ?? existing.LogoPath;
        existing.ColorCabello = request.ColorCabello ?? existing.ColorCabello;
        existing.BackgroundPath = request.BackgroundPath ?? existing.BackgroundPath;

        var updated = await _internalAvatarConfigClient.UpdateAsync(id, existing, ct);
        return Ok(MapToDtoWithUrls(updated));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var existing = await _internalAvatarConfigClient.GetByIdAsync(id, ct);
        if (existing is null)
        {
            return NotFound();
        }

        await _internalAvatarConfigClient.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/logo")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AvatarConfigDto>> UploadLogo(
        int id,
        [FromForm] AssetUploadRequest request,
        CancellationToken ct)
    {
        var config = await _internalAvatarConfigClient.GetByIdAsync(id, ct);
        if (config is null)
        {
            return NotFound();
        }

        if (request?.File is null || request.File.Length == 0)
        {
            return BadRequest("Archivo requerido.");
        }

        var blobPath = BuildLogoPath(config.Empresa, config.Sede, request.File.FileName);

        await using var stream = request.File.OpenReadStream();
        await _storage.UploadAsync(stream, blobPath, request.File.ContentType ?? "application/octet-stream", ct);

        config.LogoPath = blobPath;
        var updated = await _internalAvatarConfigClient.UpdateAsync(id, config, ct);

        return Ok(MapToDtoWithUrls(updated));
    }

    [HttpPost("{id:int}/fondo")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AvatarConfigDto>> UploadFondo(
        int id,
        [FromForm] AssetUploadRequest request,
        CancellationToken ct)
    {
        var config = await _internalAvatarConfigClient.GetByIdAsync(id, ct);
        if (config is null)
        {
            return NotFound();
        }

        if (request?.File is null || request.File.Length == 0)
        {
            return BadRequest("Archivo requerido.");
        }

        var blobPath = BuildBackgroundPath(config.Empresa, config.Sede, request.File.FileName);

        await using var stream = request.File.OpenReadStream();
        await _storage.UploadAsync(stream, blobPath, request.File.ContentType ?? "application/octet-stream", ct);

        config.BackgroundPath = blobPath;
        var updated = await _internalAvatarConfigClient.UpdateAsync(id, config, ct);

        return Ok(MapToDtoWithUrls(updated));
    }

    [HttpPost("{id:int}/model")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AvatarConfigDto>> UploadModel(
        int id,
        [FromForm] AssetUploadRequest request,
        CancellationToken ct)
    {
        var config = await _internalAvatarConfigClient.GetByIdAsync(id, ct);
        if (config is null)
        {
            return NotFound();
        }

        if (request?.File is null || request.File.Length == 0)
        {
            return BadRequest("Archivo requerido.");
        }

        var blobPath = BuildModelPath(config.Empresa, config.Sede, request.File.FileName);
        var contentType = request.File.FileName.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)
            ? "model/gltf-binary"
            : request.File.ContentType ?? "application/octet-stream";

        await using var stream = request.File.OpenReadStream();
        await _storage.UploadAsync(stream, blobPath, contentType, ct);

        config.Vestimenta = blobPath;
        var updated = await _internalAvatarConfigClient.UpdateAsync(id, config, ct);

        return Ok(MapToDtoWithUrls(updated));
    }

    private AvatarConfigDto MapToDtoWithUrls(InternalAvatarConfigDto config)
    {
        var dto = new AvatarConfigDto
        {
            Id = config.Id,
            Empresa = config.Empresa,
            Sede = config.Sede,
            Vestimenta = config.Vestimenta,
            Fondo = config.Fondo,
            Voz = config.Voz,
            Idioma = config.Idioma,
            LogoPath = config.LogoPath,
            ColorCabello = config.ColorCabello,
            BackgroundPath = config.BackgroundPath,
            IsActive = config.IsActive
        };

        dto.LogoUrl = BuildPublicUrl(config.LogoPath);
        dto.BackgroundUrl = BuildPublicUrl(config.BackgroundPath);

        return dto;
    }

    private AvatarConfigListItemDto MapToListItemWithUrls(InternalAvatarConfigDto config)
    {
        var dto = new AvatarConfigListItemDto
        {
            Id = config.Id,
            Empresa = config.Empresa,
            Sede = config.Sede,
            Vestimenta = config.Vestimenta,
            Fondo = config.Fondo,
            Voz = config.Voz,
            Idioma = config.Idioma,
            LogoPath = config.LogoPath,
            ColorCabello = config.ColorCabello,
            BackgroundPath = config.BackgroundPath,
            IsActive = config.IsActive
        };

        dto.LogoUrl = BuildPublicUrl(config.LogoPath);
        dto.BackgroundUrl = BuildPublicUrl(config.BackgroundPath);

        return dto;
    }

    private static string BuildLogoPath(string empresa, string sede, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var safeEmpresa = SanitizeSegment(empresa);

        // Usamos /archivo/... para mantener el patrón esperado por la API interna y el endpoint público.
        return $"/archivo/{safeEmpresa}/logo{extension}";
    }

    private static string BuildBackgroundPath(string empresa, string sede, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var safeEmpresa = SanitizeSegment(empresa);
        var safeSede = SanitizeSegment(sede);

        // Usamos /archivo/... para mantener el patrón esperado por la API interna y el endpoint público.
        return $"/archivo/{safeEmpresa}/{safeSede}/background{extension}";
    }

    private static string BuildModelPath(string empresa, string sede, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var fileName = string.IsNullOrWhiteSpace(extension) ? "model.glb" : $"model{extension}";
        var safeEmpresa = SanitizeSegment(empresa);
        var safeSede = SanitizeSegment(sede);

        return $"public/{safeEmpresa}/{safeSede}/models/{fileName}";
    }

    private string? BuildPublicUrl(string? blobPath)
    {
        if (string.IsNullOrWhiteSpace(blobPath))
        {
            return null;
        }

        var trimmed = blobPath.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var baseUrl = _storageOptions.BlobServiceEndpoint?.TrimEnd('/') ?? string.Empty;
        var container = _storageOptions.ContainerNamePublic?.Trim('/') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(container))
        {
            return trimmed;
        }

        return $"{baseUrl}/{container}/{trimmed.TrimStart('/')}";
    }

    private static string SanitizeSegment(string value)
    {
        var sanitized = value.Trim().ToLowerInvariant();
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(c, '-');
        }

        return sanitized.Replace(' ', '-');
    }
}
