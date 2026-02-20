using AvatarSentry.Application.AvatarConfigs;
using AvatarSentry.Application.InternalApi;
using AvatarSentry.Application.InternalApi.Clients;
using Avatar_3D_Sentry.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/avatar")]
public class AvatarPublicConfigController : ControllerBase
{
    private readonly IInternalAvatarConfigClient _internalAvatarConfigClient;
    private readonly ICompanySiteResolutionService _resolution;
    private readonly IAssetStorage _storage;

    public AvatarPublicConfigController(
        IInternalAvatarConfigClient internalAvatarConfigClient,
        ICompanySiteResolutionService resolution,
        IAssetStorage storage)
    {
        _internalAvatarConfigClient = internalAvatarConfigClient;
        _resolution = resolution;
        _storage = storage;
    }

    [AllowAnonymous]
    [HttpGet("config")]
    public async Task<ActionResult<AvatarConfigDto>> GetByScope([FromQuery] string empresa, [FromQuery] string sede, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(empresa) || string.IsNullOrWhiteSpace(sede))
        {
            return BadRequest("Debe enviar empresa y sede.");
        }

        var ids = await _resolution.ResolveToIdsAsync(empresa, sede, ct);
        if (!ids.HasValue)
        {
            return NotFound();
        }

        var config = await _internalAvatarConfigClient.GetByScopeAsync(ids.Value.CompanyId, ids.Value.SiteId, ct);
        if (config is null)
        {
            return NotFound();
        }

        var names = await _resolution.GetNamesAsync(config.CompanyId, config.SiteId, ct);
        return Ok(new AvatarConfigDto
        {
            Id = config.Id,
            Empresa = names?.CompanyName ?? empresa,
            Sede = names?.SiteName ?? sede,
            Vestimenta = config.ModelUrl,
            Fondo = config.BackgroundUrl,
            Voz = config.VoiceIds?.Length > 0 ? string.Join(",", config.VoiceIds) : null,
            Idioma = config.Language,
            LogoPath = ResolveAssetUrl(config.LogoUrl),
            LogoUrl = ResolveAssetUrl(config.LogoUrl),
            BackgroundPath = ResolveAssetUrl(config.BackgroundUrl),
            BackgroundUrl = ResolveAssetUrl(config.BackgroundUrl),
            ColorCabello = config.HairColor,
            IsActive = config.IsActive
        });
    }

    private string? ResolveAssetUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var trimmed = path.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return _storage.GetPublicUrl(trimmed.TrimStart('/'));
    }
}
