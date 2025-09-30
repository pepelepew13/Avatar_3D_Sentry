using Avatar_3D_Sentry.Data;
using Avatar_3D_Sentry.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace Avatar_3D_Sentry.Controllers;

[ApiController]
[Route("api/avatar/config")]
public class AvatarConfigController : ControllerBase
{
    private readonly AvatarContext _context;

    public AvatarConfigController(AvatarContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<AvatarConfig>> Get([FromQuery] string empresa, [FromQuery] string sede)
    {
        var config = await _context.FindByEmpresaYSedeAsync(empresa, sede, HttpContext.RequestAborted);
        if (config is null)
        {
            config = new AvatarConfig { Empresa = empresa, Sede = sede };
            _context.AvatarConfigs.Add(config);
            await _context.SaveChangesAsync(HttpContext.RequestAborted);
        }
        return Ok(config);
    }
}
