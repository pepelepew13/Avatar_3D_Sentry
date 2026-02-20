using System.IO;
using Avatar_3D_Sentry.Models;
using Avatar_3D_Sentry.Services.Storage;
using AvatarSentry.Application.AvatarConfigs;
using AvatarSentry.Application.Exceptions;
using AvatarSentry.Application.InternalApi;
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
    private readonly ICompanySiteResolutionService _resolution;
    private readonly IAssetStorage _storage;
    private readonly AzureStorageOptions _storageOptions;

    public AvatarConfigsController(
        IInternalAvatarConfigClient internalAvatarConfigClient,
        ICompanySiteResolutionService resolution,
        IAssetStorage storage,
        IOptions<AzureStorageOptions> storageOptions)
    {
        _internalAvatarConfigClient = internalAvatarConfigClient;
        _resolution = resolution;
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
        int? companyId = null, siteId = null;
        if (!string.IsNullOrWhiteSpace(empresa) || !string.IsNullOrWhiteSpace(sede))
        {
            var ids = await _resolution.ResolveToIdsAsync(empresa, sede, ct);
            if (ids.HasValue) { companyId = ids.Value.CompanyId; siteId = ids.Value.SiteId; }
        }

        var filter = new AvatarConfigFilter
        {
            Company = companyId,
            Site = siteId,
            Page = page,
            PageSize = pageSize
        };

        try
        {
            var result = await _internalAvatarConfigClient.GetConfigsAsync(filter, ct);
            var items = new List<AvatarConfigListItemDto>();
            foreach (var config in result.Items)
            {
                var names = await _resolution.GetNamesAsync(config.CompanyId, config.SiteId, ct);
                items.Add(MapToListItemWithUrls(config, names?.CompanyName, names?.SiteName));
            }

            var response = new PagedResult<AvatarConfigListItemDto>
            {
                Page = result.Page,
                PageSize = result.PageSize,
                Total = result.Total,
                Items = items
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

        var names = await _resolution.GetNamesAsync(config.CompanyId, config.SiteId, ct);
        return Ok(MapToDtoWithUrls(config, names?.CompanyName, names?.SiteName));
    }

    [HttpPost]
    public async Task<ActionResult<AvatarConfigDto>> Create([FromBody] CreateAvatarConfigRequest request, CancellationToken ct)
    {
        var ids = await _resolution.ResolveToIdsAsync(request.Empresa, request.Sede, ct);
        if (!ids.HasValue)
        {
            return BadRequest("Empresa y sede no se pudieron resolver (códigos no encontrados).");
        }

        var payload = new CreateInternalAvatarConfigRequest
        {
            CompanyId = ids.Value.CompanyId,
            SiteId = ids.Value.SiteId,
            ModelPath = request.Vestimenta,
            BackgroundPath = request.BackgroundPath,
            LogoPath = request.LogoPath,
            Language = request.Idioma,
            HairColor = request.ColorCabello,
            VoiceIds = request.VoiceIds ?? Array.Empty<int>(),
            Status = "Draft",
            IsActive = true
        };

        try
        {
            var created = await _internalAvatarConfigClient.CreateAsync(payload, ct);
            var names = await _resolution.GetNamesAsync(created.CompanyId, created.SiteId, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDtoWithUrls(created, names?.CompanyName, names?.SiteName));
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

        var ids = await _resolution.ResolveToIdsAsync(request.Empresa, request.Sede, ct);
        if (!ids.HasValue)
        {
            return BadRequest("Empresa y sede no se pudieron resolver (códigos no encontrados).");
        }

        var payload = new UpdateInternalAvatarConfigRequest
        {
            CompanyId = ids.Value.CompanyId,
            SiteId = ids.Value.SiteId,
            ModelPath = request.Vestimenta,
            BackgroundPath = request.BackgroundPath,
            LogoPath = request.LogoPath,
            Language = request.Idioma,
            HairColor = request.ColorCabello,
            VoiceIds = request.VoiceIds ?? Array.Empty<int>(),
            Status = existing.Status,
            IsActive = existing.IsActive
        };

        try
        {
            var updated = await _internalAvatarConfigClient.UpdateAsync(id, payload, ct);
            var names = await _resolution.GetNamesAsync(updated.CompanyId, updated.SiteId, ct);
            return Ok(MapToDtoWithUrls(updated, names?.CompanyName, names?.SiteName));
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

        var payload = new UpdateInternalAvatarConfigRequest
        {
            CompanyId = existing.CompanyId,
            SiteId = existing.SiteId,
            ModelPath = request.Vestimenta ?? existing.ModelUrl,
            BackgroundPath = request.BackgroundPath ?? request.Fondo ?? existing.BackgroundUrl,
            LogoPath = request.LogoPath ?? existing.LogoUrl,
            Language = request.Idioma ?? existing.Language,
            HairColor = request.ColorCabello ?? existing.HairColor,
            VoiceIds = existing.VoiceIds ?? Array.Empty<int>(),
            Status = existing.Status ?? "Draft",
            IsActive = existing.IsActive
        };

        var updated = await _internalAvatarConfigClient.UpdateAsync(id, payload, ct);
        var names = await _resolution.GetNamesAsync(updated.CompanyId, updated.SiteId, ct);
        return Ok(MapToDtoWithUrls(updated, names?.CompanyName, names?.SiteName));
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

        var names = await _resolution.GetNamesAsync(config.CompanyId, config.SiteId, ct);
        var empresa = names?.CompanyName ?? "default";
        var sede = names?.SiteName ?? "default";
        var blobPath = BuildLogoPath(empresa, sede, request.File.FileName);

        await using var stream = request.File.OpenReadStream();
        await _storage.UploadAsync(stream, blobPath, request.File.ContentType ?? "application/octet-stream", ct);

        var updateReq = new UpdateInternalAvatarConfigRequest
        {
            CompanyId = config.CompanyId,
            SiteId = config.SiteId,
            ModelPath = config.ModelUrl,
            BackgroundPath = config.BackgroundUrl,
            LogoPath = blobPath,
            Language = config.Language,
            HairColor = config.HairColor,
            VoiceIds = config.VoiceIds ?? Array.Empty<int>(),
            Status = config.Status ?? "Draft",
            IsActive = config.IsActive
        };
        var updated = await _internalAvatarConfigClient.UpdateAsync(id, updateReq, ct);
        return Ok(MapToDtoWithUrls(updated, names?.CompanyName, names?.SiteName));
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

        var names = await _resolution.GetNamesAsync(config.CompanyId, config.SiteId, ct);
        var empresa = names?.CompanyName ?? "default";
        var sede = names?.SiteName ?? "default";
        var blobPath = BuildBackgroundPath(empresa, sede, request.File.FileName);

        await using var stream = request.File.OpenReadStream();
        await _storage.UploadAsync(stream, blobPath, request.File.ContentType ?? "application/octet-stream", ct);

        var updateReq = new UpdateInternalAvatarConfigRequest
        {
            CompanyId = config.CompanyId,
            SiteId = config.SiteId,
            ModelPath = config.ModelUrl,
            BackgroundPath = blobPath,
            LogoPath = config.LogoUrl,
            Language = config.Language,
            HairColor = config.HairColor,
            VoiceIds = config.VoiceIds ?? Array.Empty<int>(),
            Status = config.Status ?? "Draft",
            IsActive = config.IsActive
        };
        var updated = await _internalAvatarConfigClient.UpdateAsync(id, updateReq, ct);
        return Ok(MapToDtoWithUrls(updated, names?.CompanyName, names?.SiteName));
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

        var names = await _resolution.GetNamesAsync(config.CompanyId, config.SiteId, ct);
        var empresa = names?.CompanyName ?? "default";
        var sede = names?.SiteName ?? "default";
        var blobPath = BuildModelPath(empresa, sede, request.File.FileName);
        var contentType = request.File.FileName.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)
            ? "model/gltf-binary"
            : request.File.ContentType ?? "application/octet-stream";

        await using var stream = request.File.OpenReadStream();
        await _storage.UploadAsync(stream, blobPath, contentType, ct);

        var updateReq = new UpdateInternalAvatarConfigRequest
        {
            CompanyId = config.CompanyId,
            SiteId = config.SiteId,
            ModelPath = blobPath,
            BackgroundPath = config.BackgroundUrl,
            LogoPath = config.LogoUrl,
            Language = config.Language,
            HairColor = config.HairColor,
            VoiceIds = config.VoiceIds ?? Array.Empty<int>(),
            Status = config.Status ?? "Draft",
            IsActive = config.IsActive
        };
        var updated = await _internalAvatarConfigClient.UpdateAsync(id, updateReq, ct);
        return Ok(MapToDtoWithUrls(updated, names?.CompanyName, names?.SiteName));
    }

    private AvatarConfigDto MapToDtoWithUrls(InternalAvatarConfigDto config, string? empresa, string? sede)
    {
        var expiry = config.UrlExpiresAtUtc ?? DateTime.UtcNow.AddMinutes(_storageOptions.SasExpiryMinutes);
        var dto = new AvatarConfigDto
        {
            Id = config.Id,
            Empresa = empresa ?? string.Empty,
            Sede = sede ?? string.Empty,
            Vestimenta = config.ModelUrl,
            Fondo = config.BackgroundUrl,
            Voz = config.VoiceIds?.Length > 0 ? string.Join(",", config.VoiceIds) : null,
            Idioma = config.Language,
            LogoPath = config.LogoUrl,
            ColorCabello = config.HairColor,
            BackgroundPath = config.BackgroundUrl,
            UrlExpiresAtUtc = expiry,
            IsActive = config.IsActive
        };

        dto.LogoUrl = BuildAssetUrl(config.LogoUrl);
        dto.BackgroundUrl = BuildAssetUrl(config.BackgroundUrl);

        return dto;
    }

    private AvatarConfigListItemDto MapToListItemWithUrls(InternalAvatarConfigDto config, string? empresa, string? sede)
    {
        var expiry = config.UrlExpiresAtUtc ?? DateTime.UtcNow.AddMinutes(_storageOptions.SasExpiryMinutes);
        var dto = new AvatarConfigListItemDto
        {
            Id = config.Id,
            Empresa = empresa ?? string.Empty,
            Sede = sede ?? string.Empty,
            Vestimenta = config.ModelUrl,
            Fondo = config.BackgroundUrl,
            Voz = config.VoiceIds?.Length > 0 ? string.Join(",", config.VoiceIds) : null,
            Idioma = config.Language,
            LogoPath = config.LogoUrl,
            ColorCabello = config.HairColor,
            BackgroundPath = config.BackgroundUrl,
            UrlExpiresAtUtc = expiry,
            IsActive = config.IsActive
        };

        dto.LogoUrl = BuildAssetUrl(config.LogoUrl);
        dto.BackgroundUrl = BuildAssetUrl(config.BackgroundUrl);

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

    /// <summary>Genera URL de lectura (SAS si Azure, o estática si local) para un asset. Path es la fuente de verdad.</summary>
    private string? BuildAssetUrl(string? blobPath)
    {
        if (string.IsNullOrWhiteSpace(blobPath))
        {
            return null;
        }

        var trimmed = blobPath.Trim().TrimStart('/');
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return blobPath.Trim();
        }

        return _storage.GetPublicUrl(blobPath.TrimStart('/'), TimeSpan.FromMinutes(_storageOptions.SasExpiryMinutes));
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
