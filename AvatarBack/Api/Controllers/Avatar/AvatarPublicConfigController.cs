using AvatarSentry.Application.AvatarConfigs;
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
    private readonly IAssetStorage _storage;

    public AvatarPublicConfigController(
        IInternalAvatarConfigClient internalAvatarConfigClient,
        IAssetStorage storage)
    {
        _internalAvatarConfigClient = internalAvatarConfigClient;
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

        var config = await _internalAvatarConfigClient.GetByScopeAsync(empresa, sede, ct);
        if (config is null)
        {
            return NotFound();
        }

        return Ok(new AvatarConfigDto
        {
            Id = config.Id,
            Empresa = config.Empresa,
            Sede = config.Sede,
            Vestimenta = config.Vestimenta,
            Fondo = config.Fondo,
            Voz = config.Voz,
            Idioma = config.Idioma,
            LogoPath = ResolveAssetUrl(config.LogoPath),
            BackgroundPath = ResolveAssetUrl(config.BackgroundPath),
            ColorCabello = config.ColorCabello,
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
