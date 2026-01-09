using AvatarSentry.Application.AvatarConfigs;
using AvatarSentry.Application.InternalApi.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/avatar")]
public class AvatarPublicConfigController : ControllerBase
{
    private readonly IInternalAvatarConfigClient _internalAvatarConfigClient;

    public AvatarPublicConfigController(IInternalAvatarConfigClient internalAvatarConfigClient)
    {
        _internalAvatarConfigClient = internalAvatarConfigClient;
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
            LogoPath = config.LogoPath
        });
    }
}
