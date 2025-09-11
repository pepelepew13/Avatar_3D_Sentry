using Avatar_3D_Sentry.Data;
using Avatar_3D_Sentry.Modelos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        var config = await _context.AvatarConfigs
            .FirstOrDefaultAsync(c => c.Empresa == empresa && c.Sede == sede);
        return config is null ? NotFound() : Ok(config);
    }
}
