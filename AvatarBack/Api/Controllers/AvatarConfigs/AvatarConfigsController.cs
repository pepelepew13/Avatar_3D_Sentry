using System.Security.Claims;
using System.IO;
using Avatar_3D_Sentry.Models;
using Avatar_3D_Sentry.Services.Storage;
using AvatarSentry.Application.AvatarConfigs;
using AvatarSentry.Application.InternalApi.Clients;
using AvatarSentry.Application.InternalApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/avatar-configs")]
[Authorize]
public class AvatarConfigsController : ControllerBase
{
    private readonly IInternalAvatarConfigClient _internalAvatarConfigClient;
    private readonly IAssetStorage _storage;

    public AvatarConfigsController(
        IInternalAvatarConfigClient internalAvatarConfigClient,
        IAssetStorage storage)
    {
        _internalAvatarConfigClient = internalAvatarConfigClient;
        _storage = storage;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AvatarConfigDto>>> GetConfigs(
        [FromQuery] string? empresa,
        [FromQuery] string? sede,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var (scopeEmpresa, scopeSede, isGlobalAdmin) = GetScope();
        if (!isGlobalAdmin)
        {
            if (string.IsNullOrWhiteSpace(scopeEmpresa) || string.IsNullOrWhiteSpace(scopeSede))
            {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(empresa) && !string.Equals(empresa, scopeEmpresa, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(sede) && !string.Equals(sede, scopeSede, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }
        }

        var filter = new AvatarConfigFilter
        {
            Empresa = isGlobalAdmin ? empresa : scopeEmpresa,
            Sede = isGlobalAdmin ? sede : scopeSede,
            Page = page,
            PageSize = pageSize
        };

        var result = await _internalAvatarConfigClient.GetConfigsAsync(filter, ct);
        var response = new PagedResponse<AvatarConfigDto>
        {
            Page = result.Page,
            PageSize = result.PageSize,
            Total = result.Total,
            Items = result.Items.Select(MapToDto).ToList()
        };

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AvatarConfigDto>> GetById(int id, CancellationToken ct)
    {
        var config = await _internalAvatarConfigClient.GetByIdAsync(id, ct);
        if (config is null)
        {
            return NotFound();
        }

        if (!CanAccess(config))
        {
            return Forbid();
        }

        return Ok(MapToDto(config));
    }

    [HttpPost]
    public async Task<ActionResult<AvatarConfigDto>> Create([FromBody] CreateAvatarConfigRequest request, CancellationToken ct)
    {
        if (!CanAccess(request.Empresa, request.Sede))
        {
            return Forbid();
        }

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

        var created = await _internalAvatarConfigClient.CreateAsync(payload, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AvatarConfigDto>> Update(int id, [FromBody] UpdateAvatarConfigRequest request, CancellationToken ct)
    {
        if (!CanAccess(request.Empresa, request.Sede))
        {
            return Forbid();
        }

        var existing = await _internalAvatarConfigClient.GetByIdAsync(id, ct);
        if (existing is null)
        {
            return NotFound();
        }

        if (!CanAccess(existing))
        {
            return Forbid();
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

        var updated = await _internalAvatarConfigClient.UpdateAsync(id, payload, ct);
        return Ok(MapToDto(updated));
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<AvatarConfigDto>> Patch(int id, [FromBody] AvatarConfigPatchRequest request, CancellationToken ct)
    {
        var existing = await _internalAvatarConfigClient.GetByIdAsync(id, ct);
        if (existing is null)
        {
            return NotFound();
        }

        if (!CanAccess(existing))
        {
            return Forbid();
        }

        existing.Vestimenta = request.Vestimenta ?? existing.Vestimenta;
        existing.Fondo = request.Fondo ?? existing.Fondo;
        existing.Voz = request.Voz ?? existing.Voz;
        existing.Idioma = request.Idioma ?? existing.Idioma;
        existing.LogoPath = request.LogoPath ?? existing.LogoPath;
        existing.ColorCabello = request.ColorCabello ?? existing.ColorCabello;
        existing.BackgroundPath = request.BackgroundPath ?? existing.BackgroundPath;

        var updated = await _internalAvatarConfigClient.UpdateAsync(id, existing, ct);
        return Ok(MapToDto(updated));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var existing = await _internalAvatarConfigClient.GetByIdAsync(id, ct);
        if (existing is null)
        {
            return NotFound();
        }

        if (!CanAccess(existing))
        {
            return Forbid();
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

        if (!CanAccess(config))
        {
            return Forbid();
        }

        if (request?.File is null || request.File.Length == 0)
        {
            return BadRequest("Archivo requerido.");
        }

        var blobPath = BuildBrandingPath(config.Empresa, config.Sede, "logo", request.File.FileName);

        await using var stream = request.File.OpenReadStream();
        await _storage.UploadAsync(stream, blobPath, request.File.ContentType ?? "application/octet-stream", ct);

        config.LogoPath = blobPath;
        var updated = await _internalAvatarConfigClient.UpdateAsync(id, config, ct);

        return Ok(MapToDto(updated));
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

        if (!CanAccess(config))
        {
            return Forbid();
        }

        if (request?.File is null || request.File.Length == 0)
        {
            return BadRequest("Archivo requerido.");
        }

        var blobPath = BuildBrandingPath(config.Empresa, config.Sede, "fondo", request.File.FileName);

        await using var stream = request.File.OpenReadStream();
        await _storage.UploadAsync(stream, blobPath, request.File.ContentType ?? "application/octet-stream", ct);

        config.BackgroundPath = blobPath;
        var updated = await _internalAvatarConfigClient.UpdateAsync(id, config, ct);

        return Ok(MapToDto(updated));
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

        if (!CanAccess(config))
        {
            return Forbid();
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

        return Ok(MapToDto(updated));
    }

    private (string? Empresa, string? Sede, bool IsGlobalAdmin) GetScope()
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var empresa = User.FindFirst("empresa")?.Value;
        var sede = User.FindFirst("sede")?.Value;
        var isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        var isGlobalAdmin = isAdmin && string.IsNullOrWhiteSpace(empresa) && string.IsNullOrWhiteSpace(sede);

        return (empresa, sede, isGlobalAdmin);
    }

    private bool CanAccess(InternalAvatarConfigDto config)
    {
        return CanAccess(config.Empresa, config.Sede);
    }

    private bool CanAccess(string empresa, string sede)
    {
        var (scopeEmpresa, scopeSede, isGlobalAdmin) = GetScope();
        if (isGlobalAdmin)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(scopeEmpresa) || string.IsNullOrWhiteSpace(scopeSede))
        {
            return false;
        }

        return string.Equals(scopeEmpresa, empresa, StringComparison.OrdinalIgnoreCase)
               && string.Equals(scopeSede, sede, StringComparison.OrdinalIgnoreCase);
    }

    private static AvatarConfigDto MapToDto(InternalAvatarConfigDto config) => new()
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

    private static string BuildBrandingPath(string empresa, string sede, string assetName, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var safeEmpresa = SanitizeSegment(empresa);
        var safeSede = SanitizeSegment(sede);
        var safeAssetName = SanitizeSegment(assetName);

        // ✅ public/{empresa}/{sede}/branding/...
        return $"public/{safeEmpresa}/{safeSede}/branding/{safeAssetName}{extension}";
    }

    private static string BuildModelPath(string empresa, string sede, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var fileName = string.IsNullOrWhiteSpace(extension) ? "model.glb" : $"model{extension}";
        var safeEmpresa = SanitizeSegment(empresa);
        var safeSede = SanitizeSegment(sede);

        // ✅ public/{empresa}/{sede}/models/...
        return $"public/{safeEmpresa}/{safeSede}/models/{fileName}";
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
